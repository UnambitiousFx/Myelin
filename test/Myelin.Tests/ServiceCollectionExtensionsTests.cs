using Microsoft.Extensions.DependencyInjection;

namespace UnambitiousFx.Myelin.Tests;

public class ServiceCollectionExtensionsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddMyelin_WithConfiguration_RegistersAClientThatAppliesIt()
    {
        // Arrange (Given)
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddMyelin(o => o.AllowedHosts = ["api.example.com"])
            .BuildServiceProvider();
        var client = provider.GetRequiredService<SafeHttpClient>();

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(() => client.GetAsync(new Uri("https://evil.example/"), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.HostNotAllowed, refusal.Reason);
        Assert.Same(client, provider.GetRequiredService<SafeHttpClient>());
    }

    [Fact]
    public async Task AddMyelin_WithRegisteredResolver_UsesItForTheAddressChecks()
    {
        // Arrange (Given)
        var resolver = new FixedResolver("10.0.0.1");
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHostResolver>(resolver)
            .AddMyelin()
            .BuildServiceProvider();

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(
            () => provider.GetRequiredService<SafeHttpClient>().GetAsync(new Uri("https://api.example.com/"), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal(["api.example.com"], resolver.Lookups);
    }

    [Theory]
    [InlineData("http://api.example.com/", RefusalReasons.NotHttps)]
    [InlineData("https://evil.example/", RefusalReasons.HostNotAllowed)]
    [InlineData("https://user:pw@api.example.com/", RefusalReasons.UserInfo)]
    [InlineData("https://api.example.com/", RefusalReasons.AddressRefused)]
    public async Task UseMyelin_OnFactoryClient_RefusesUnsafeRequests(string url, string reason)
    {
        // Arrange (Given)
        using var provider = new ServiceCollection()
            .AddSingleton<IHostResolver>(new FixedResolver("169.254.169.254"))
            .AddHttpClient("webhooks")
            .UseMyelin(o => o.AllowedHosts = ["api.example.com"])
            .Services
            .BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("webhooks");

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(() => client.GetAsync(url, Ct));

        // Assert (Then)
        Assert.Equal(reason, refusal.Reason);
    }

    [Fact]
    public async Task UseMyelin_OptionsOfOneClient_DoNotApplyToAnother()
    {
        // Arrange (Given)
        var resolver = new FixedResolver("127.0.0.1");
        var services = new ServiceCollection().AddSingleton<IHostResolver>(resolver);
        services.AddHttpClient("strict").UseMyelin(o => o.AllowedHosts = ["api.example.com"]);
        services.AddHttpClient("open").UseMyelin();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act (When)
        var strict = await Assert.ThrowsAsync<OutboundRefusedException>(() => factory.CreateClient("strict").GetAsync("https://other.example/", Ct));
        var open = await Assert.ThrowsAsync<OutboundRefusedException>(() => factory.CreateClient("open").GetAsync("https://other.example/", Ct));

        // Assert (Then): the open client passes the allow-list and stops at the address check.
        Assert.Equal(RefusalReasons.HostNotAllowed, strict.Reason);
        Assert.Equal(RefusalReasons.AddressRefused, open.Reason);
    }
}
