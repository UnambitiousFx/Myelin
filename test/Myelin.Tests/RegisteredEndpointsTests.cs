using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnambitiousFx.Myelin.Tests;

public class RegisteredEndpointsTests
{
    private const string Worker = "worker.lan";
    private static readonly Uri WorkerUrl = new("https://worker.lan:8443/ocr");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SafeHttpClient Client(HttpMessageHandler handler)
    {
        return new SafeHttpClient(new HttpMessageInvoker(handler), new MyelinOptions(), NullLogger<SafeHttpClient>.Instance);
    }

    private static ValueTask<bool> IsPermitted(Uri url, IRegisteredEndpoints? registered, params string[] answers)
    {
        return MyelinHandler.IsPermittedAsync(url.IdnHost, url.Port, url, answers.Select(IPAddress.Parse).ToArray(), registered, Ct);
    }

    [Theory]
    [InlineData("10.0.0.1")] // private-use
    [InlineData("172.16.0.1")] // private-use
    [InlineData("172.31.255.254")] // private-use
    [InlineData("192.168.1.10")] // private-use
    [InlineData("100.64.0.1")] // shared address space (CGNAT)
    [InlineData("100.101.102.103")] // a Tailscale address
    [InlineData("fc00::1")] // unique local
    [InlineData("fd7a:115c:a1e0::1")] // a Tailscale ULA
    [InlineData("::ffff:192.168.1.10")] // IPv4-mapped private-use
    [InlineData("8.8.8.8")] // public stays allowed
    public void IsAllowedForRegisteredEndpoint_WithPrivateNetworkAddress_Allows(string address)
    {
        // Act (When)
        var allowed = AddressPolicy.IsAllowedForRegisteredEndpoint(IPAddress.Parse(address));

        // Assert (Then)
        Assert.True(allowed);
    }

    [Theory]
    [InlineData("127.0.0.1")] // loopback
    [InlineData("::1")] // loopback
    [InlineData("169.254.169.254")] // link-local, cloud metadata
    [InlineData("169.254.1.1")] // link-local
    [InlineData("fe80::1")] // link-local
    [InlineData("fd00:ec2::254")] // AWS metadata, inside fc00::/7
    [InlineData("100.100.100.200")] // Alibaba Cloud metadata, inside 100.64.0.0/10
    [InlineData("0.0.0.0")] // unspecified
    [InlineData("::")] // unspecified
    [InlineData("224.0.0.1")] // multicast
    [InlineData("ff02::1")] // multicast
    [InlineData("255.255.255.255")] // broadcast
    [InlineData("192.0.2.1")] // documentation
    [InlineData("198.18.0.1")] // benchmarking
    [InlineData("fec0::1")] // deprecated site-local
    [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
    [InlineData("64:ff9b::a00:1")] // NAT64 of 10.0.0.1: a translator, not the registered host
    public void IsAllowedForRegisteredEndpoint_WithOtherRefusedAddress_Refuses(string address)
    {
        // Act (When)
        var allowed = AddressPolicy.IsAllowedForRegisteredEndpoint(IPAddress.Parse(address));

        // Assert (Then)
        Assert.False(allowed);
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("fd12::1")]
    [InlineData("10.0.0.1", "8.8.8.8")]
    public async Task IsPermittedAsync_WithRegisteredEndpointOnPrivateNetwork_Permits(params string[] answers)
    {
        // Arrange (Given)
        var registered = new RegisteredSet((Worker, 8443));

        // Act (When)
        var permitted = await IsPermitted(WorkerUrl, registered, answers);

        // Assert (Then)
        Assert.True(permitted);
        Assert.Equal([(Worker, 8443)], registered.Asked);
    }

    [Theory]
    [InlineData("https://worker.lan:9443/ocr")] // wrong port
    [InlineData("https://worker.lan/ocr")] // default port 443, not 8443
    [InlineData("https://other.lan:8443/ocr")] // wrong host
    [InlineData("https://sub.worker.lan:8443/ocr")] // no wildcard
    public async Task IsPermittedAsync_WithEndpointNotExactlyRegistered_Refuses(string url)
    {
        // Arrange (Given)
        var registered = new RegisteredSet((Worker, 8443));

        // Act (When)
        var permitted = await IsPermitted(new Uri(url), registered, "10.0.0.1");

        // Assert (Then)
        Assert.False(permitted);
    }

    [Fact]
    public async Task IsPermittedAsync_WithHttpScheme_RefusesWithoutAsking()
    {
        // Arrange (Given)
        var registered = new RegisteredSet((Worker, 8443));

        // Act (When)
        var permitted = await IsPermitted(new Uri("http://worker.lan:8443/ocr"), registered, "10.0.0.1");

        // Assert (Then)
        Assert.False(permitted);
        Assert.Empty(registered.Asked);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("10.0.0.1", "127.0.0.1")] // one refused answer is enough
    [InlineData("fd00:ec2::254")]
    public async Task IsPermittedAsync_WithRegisteredEndpointOnForbiddenRange_RefusesWithoutAsking(params string[] answers)
    {
        // Arrange (Given)
        var registered = new RegisteredSet((Worker, 8443));

        // Act (When)
        var permitted = await IsPermitted(WorkerUrl, registered, answers);

        // Assert (Then)
        Assert.False(permitted);
        Assert.Empty(registered.Asked);
    }

    [Fact]
    public async Task IsPermittedAsync_WithPublicAddresses_PermitsWithoutAsking()
    {
        // Arrange (Given)
        var registered = new RegisteredSet();

        // Act (When)
        var permitted = await IsPermitted(new Uri("https://api.example.com/"), registered, "8.8.8.8");

        // Assert (Then)
        Assert.True(permitted);
        Assert.Empty(registered.Asked);
    }

    [Fact]
    public async Task IsPermittedAsync_WithoutRegisteredEndpoints_RefusesPrivateAddress()
    {
        // Act (When)
        var permitted = await IsPermitted(WorkerUrl, null, "10.0.0.1");

        // Assert (Then)
        Assert.False(permitted);
    }

    [Fact]
    public async Task Create_WithRegisteredEndpointRebindingToLoopback_RefusesTheConnection()
    {
        // Arrange (Given): the registered name now answers loopback.
        using var handler = MyelinHandler.Create(new FixedResolver("127.0.0.1"), new RegisteredSet((Worker, 8443)));

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(() => Client(handler).GetAsync(WorkerUrl, 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal(Worker, refusal.Host);
    }

    [Fact]
    public async Task Create_WithEndpointRevokedAtRuntime_RefusesTheNextConnection()
    {
        // Arrange (Given)
        var registered = new RegisteredSet((Worker, 8443));
        var resolver = new FixedResolver("10.0.0.1");
        using var handler = MyelinHandler.Create(resolver, registered);
        Assert.True(await IsPermitted(WorkerUrl, registered, "10.0.0.1"));

        // Act (When)
        registered.Endpoints.Clear();
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(() => Client(handler).GetAsync(WorkerUrl, 1024, Ct));

        // Assert (Then): the set was asked again at connect time, not cached.
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal([(Worker, 8443), (Worker, 8443)], registered.Asked);
    }

    [Fact]
    public async Task Create_WithRedirectFromRegisteredToOtherPrivateHost_RefusesTheRedirect()
    {
        // Arrange (Given): the registered worker answers itself and redirects to a host on the same private network.
        var registered = new RegisteredSet((Worker, 8443));
        var resolver = new FixedResolver("10.0.0.2");
        using var myelin = MyelinHandler.Create(resolver, registered);
        using var handler = new OneHostHandler(Worker, _ => Responses.Redirect("https://admin.lan/"), myelin);

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(() => Client(handler).GetAsync(WorkerUrl, 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal("admin.lan", refusal.Host);
        Assert.Equal([("admin.lan", 443)], registered.Asked);
    }

    [Fact]
    public void Create_WithRegisteredEndpoints_BoundsPooledConnectionLifetime()
    {
        // Act (When)
        using var registered = MyelinHandler.Create(null, new RegisteredSet());
        using var plain = MyelinHandler.Create();

        // Assert (Then)
        Assert.Equal(TimeSpan.FromMinutes(1), registered.PooledConnectionLifetime);
        Assert.Equal(Timeout.InfiniteTimeSpan, plain.PooledConnectionLifetime);
    }

    [Fact]
    public async Task AddMyelin_WithRegisteredEndpoints_AsksThemAtConnectTime()
    {
        // Arrange (Given)
        var registered = new RegisteredSet();
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHostResolver>(new FixedResolver("10.0.0.1"))
            .AddSingleton<IRegisteredEndpoints>(registered)
            .AddMyelin(o => o.AllowRegisteredEndpoints(sp => sp.GetRequiredService<IRegisteredEndpoints>()))
            .BuildServiceProvider();

        // Act (When)
        var refusal = await Assert.ThrowsAsync<OutboundRefusedException>(
            () => provider.GetRequiredService<SafeHttpClient>().GetAsync(WorkerUrl, 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.AddressRefused, refusal.Reason);
        Assert.Equal([(Worker, 8443)], registered.Asked);
    }

    [Fact]
    public async Task UseMyelin_WithRegisteredEndpointsAndCustomTls_KeepsTheTlsSettingsAndAsksAtConnectTime()
    {
        // Arrange (Given): a client certificate and a pinned-key server check, configured after UseMyelin.
        var registered = new RegisteredSet();
        using var key = RSA.Create(2048);
        using var certificate = new CertificateRequest("CN=nid", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        var services = new ServiceCollection()
            .AddSingleton<IHostResolver>(new FixedResolver("10.0.0.1"))
            .AddSingleton<IRegisteredEndpoints>(registered);
        services.AddHttpClient("workers")
            .UseMyelin(o => o.AllowRegisteredEndpoints(sp => sp.GetRequiredService<IRegisteredEndpoints>()))
            .ConfigurePrimaryHttpMessageHandler((handler, _) =>
            {
                var sockets = (SocketsHttpHandler)handler;
                sockets.SslOptions.ClientCertificates = [certificate];
                sockets.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            });
        services.AddHttpClient("plain").UseMyelin();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act (When)
        var workers = await Assert.ThrowsAsync<OutboundRefusedException>(() => factory.CreateClient("workers").GetAsync(WorkerUrl, Ct));
        var plain = await Assert.ThrowsAsync<OutboundRefusedException>(() => factory.CreateClient("plain").GetAsync(WorkerUrl, Ct));

        // Assert (Then): only the opted-in client asked; both still refused an unregistered private endpoint.
        Assert.Equal(RefusalReasons.AddressRefused, workers.Reason);
        Assert.Equal(RefusalReasons.AddressRefused, plain.Reason);
        Assert.Equal([(Worker, 8443)], registered.Asked);
        var primary = PrimaryOf(provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("workers"));
        Assert.Same(certificate, primary.SslOptions.ClientCertificates![0]);
        Assert.NotNull(primary.SslOptions.RemoteCertificateValidationCallback);
        Assert.NotNull(primary.ConnectCallback);
    }

    private static SocketsHttpHandler PrimaryOf(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        return (SocketsHttpHandler)handler;
    }
}
