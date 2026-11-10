using Microsoft.Extensions.Logging.Abstractions;

namespace UnambitiousFx.Myelin.Tests;

public class MyelinHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SafeHttpClient Client(HttpMessageHandler handler)
    {
        return new SafeHttpClient(new HttpMessageInvoker(handler), new MyelinOptions(), NullLogger<SafeHttpClient>.Instance);
    }

    private static async Task<OutboundRefusedException> RefusalOf(Task task)
    {
        return await Assert.ThrowsAsync<OutboundRefusedException>(() => task);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("8.8.8.8", "10.0.0.1")] // one refused answer is enough
    public async Task Create_WithNameResolvingToRefusedAddress_RefusesTheConnection(params string[] answers)
    {
        // Arrange (Given)
        using var handler = MyelinHandler.Create(new FixedResolver(answers));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri("https://api.example.com/"), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal("api.example.com", refusal.Host);
    }

    [Fact]
    public async Task Create_WithNameResolvingToNothing_RefusesTheConnection()
    {
        // Arrange (Given)
        using var handler = MyelinHandler.Create(new FixedResolver());

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri("https://api.example.com/"), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
    }

    [Fact]
    public async Task Create_ResolvesTheHostExactlyOnceThroughItsResolver()
    {
        // Arrange (Given): the connection target comes from the one lookup that was checked, never from a
        // second resolution by the socket layer, which is what DNS rebinding relies on.
        var resolver = new FixedResolver("127.0.0.1");
        using var handler = MyelinHandler.Create(resolver);

        // Act (When)
        await RefusalOf(Client(handler).GetAsync(new Uri("https://rebind.example.com/"), 1024, Ct));

        // Assert (Then)
        Assert.Equal(["rebind.example.com"], resolver.Lookups);
    }

    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("https://[::1]/")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://0x7f000001/")] // 127.0.0.1 in hexadecimal
    [InlineData("https://2130706433/")] // 127.0.0.1 as a decimal integer
    public async Task Create_WithSystemResolver_RefusesLoopbackAndMetadataTargets(string url)
    {
        // Arrange (Given)
        using var handler = MyelinHandler.Create();

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(url), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
    }

    [Fact]
    public void Create_DisablesRedirectsCookiesAndProxies()
    {
        // Act (When)
        using var handler = MyelinHandler.Create();

        // Assert (Then)
        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseCookies);
        Assert.False(handler.UseProxy);
        Assert.NotNull(handler.ConnectCallback);
    }
}
