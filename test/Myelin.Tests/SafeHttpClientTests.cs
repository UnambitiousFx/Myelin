using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnambitiousFx.Myelin.Tests;

public class SafeHttpClientTests
{
    private const string Allowed = "https://api.example.com/file.json";

    private static SafeHttpClient Client(HttpMessageHandler handler, MyelinOptions? options = null)
    {
        options ??= new MyelinOptions { AllowedHosts = ["api.example.com", "cdn.example.org"] };
        return new SafeHttpClient(new HttpMessageInvoker(handler), options, NullLogger<SafeHttpClient>.Instance);
    }

    private static async Task<string> RefusalOf(Task task)
    {
        return (await Assert.ThrowsAsync<OutboundRefusedException>(() => task)).Reason;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAsync_WithAllowedHost_ReturnsTheBody()
    {
        // Arrange (Given)
        var client = Client(new StubHandler(_ => Responses.Ok("hello")));

        // Act (When)
        var response = await client.GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("hello", Encoding.UTF8.GetString(response.Body));
    }

    [Fact]
    public async Task GetAsync_WithErrorStatus_ReturnsItInsteadOfThrowing()
    {
        // Arrange (Given)
        var client = Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        // Act (When)
        var response = await client.GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
    }

    [Theory]
    [InlineData("http://api.example.com/file.json", RefusalReasons.NotHttps)]
    [InlineData("ftp://api.example.com/file.json", RefusalReasons.NotHttps)]
    [InlineData("https://evil.example/file.json", RefusalReasons.HostNotAllowed)]
    [InlineData("https://user:pw@api.example.com/file.json", RefusalReasons.UserInfo)]
    public async Task GetAsync_WithRefusedUrl_RefusesBeforeSending(string url, string reason)
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Ok("never"));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(url), 1024, Ct));

        // Assert (Then)
        Assert.Equal(reason, refusal);
        Assert.Empty(handler.Requested);
    }

    [Fact]
    public async Task GetAsync_WithRelativeUrl_RefusesAsNotHttps()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Ok("never"));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri("/file.json", UriKind.Relative), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.NotHttps, refusal);
    }

    [Fact]
    public async Task GetAsync_WithRedirectToAllowedHost_FollowsItAndRevalidatesEachHop()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Redirect("https://cdn.example.org/next"), _ => Responses.Ok("there"));

        // Act (When)
        var response = await Client(handler).GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal("there", Encoding.UTF8.GetString(response.Body));
        Assert.Equal([Allowed, "https://cdn.example.org/next"], handler.Requested.Select(u => u.ToString()).ToList());
    }

    [Fact]
    public async Task GetAsync_WithRelativeRedirect_ResolvesItAgainstTheCurrentUrl()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Redirect("/moved"), _ => Responses.Ok("moved"));

        // Act (When)
        await Client(handler).GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal("https://api.example.com/moved", handler.Requested[1].ToString());
    }

    [Theory]
    [InlineData("https://169.254.169.254/latest/meta-data", RefusalReasons.HostNotAllowed)]
    [InlineData("http://api.example.com/downgraded", RefusalReasons.NotHttps)]
    [InlineData("https://evil.example/", RefusalReasons.HostNotAllowed)]
    public async Task GetAsync_WithRedirectThatWouldNotBeAllowedDirectly_Refuses(string location, string reason)
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Redirect(location));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(Allowed), 1024, Ct));

        // Assert (Then)
        Assert.Equal(reason, refusal);
        Assert.Single(handler.Requested);
    }

    [Fact]
    public async Task GetAsync_WithRedirectLoop_StopsAfterMaxRedirects()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Redirect(Allowed));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(Allowed), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.TooManyRedirects, refusal);
        Assert.Equal(4, handler.Requested.Count); // the request and its 3 allowed redirects
    }

    [Fact]
    public async Task GetAsync_WithRedirectWithoutLocation_Refuses()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Found));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(Allowed), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.RedirectWithoutLocation, refusal);
    }

    [Fact]
    public async Task GetAsync_WithDeclaredBodyOverTheLimit_Refuses()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Ok(new string('x', 2048)));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(Allowed), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.TooLarge, refusal);
    }

    [Fact]
    public async Task GetAsync_WithStreamedBodyOverTheLimit_RefusesWhileReading()
    {
        // Arrange (Given): no Content-Length, the limit is enforced on what is read.
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(new byte[2048]))
        });

        // Act (When)
        var refusal = await RefusalOf(Client(handler).GetAsync(new Uri(Allowed), 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.TooLarge, refusal);
    }

    [Fact]
    public async Task GetAsync_WithBodyAtTheLimit_ReturnsIt()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Ok(new string('x', 1024)));

        // Act (When)
        var response = await Client(handler).GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal(1024, response.Body.Length);
    }

    [Fact]
    public async Task GetAsync_WithNoAllowList_AllowsAnyHttpsHost()
    {
        // Arrange (Given): the address checks still apply, in the handler.
        var handler = new StubHandler(_ => Responses.Ok("ok"));
        var client = Client(handler, new MyelinOptions());

        // Act (When)
        var response = await client.GetAsync(new Uri("https://anything.example.net/"), 1024, Ct);

        // Assert (Then)
        Assert.Equal(HttpStatusCode.OK, response.Status);
    }

    [Theory]
    [InlineData("https://a.example.com/", true)]
    [InlineData("https://a.b.example.com/", true)]
    [InlineData("https://A.EXAMPLE.COM/", true)]
    [InlineData("https://example.com/", false)]
    [InlineData("https://badexample.com/", false)]
    [InlineData("https://example.com.evil.net/", false)]
    public async Task GetAsync_WithWildcardEntry_AllowsSubdomainsOnly(string url, bool allowed)
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Ok("ok"));
        var client = Client(handler, new MyelinOptions { AllowedHosts = ["*.example.com"] });

        // Act (When)
        var call = client.GetAsync(new Uri(url), 1024, Ct);

        // Assert (Then)
        if (allowed)
        {
            Assert.Equal(HttpStatusCode.OK, (await call).Status);
        }
        else
        {
            Assert.Equal(RefusalReasons.HostNotAllowed, await RefusalOf(call));
        }
    }

    [Fact]
    public async Task GetAsync_WithInternationalizedHost_ComparesItsPunycodeForm()
    {
        // Arrange (Given): "bücher.example" is "xn--bcher-kva.example" on the wire.
        var handler = new StubHandler(_ => Responses.Ok("ok"));
        var client = Client(handler, new MyelinOptions { AllowedHosts = ["xn--bcher-kva.example"] });

        // Act (When)
        var response = await client.GetAsync(new Uri("https://bücher.example/"), 1024, Ct);

        // Assert (Then)
        Assert.Equal(HttpStatusCode.OK, response.Status);
    }

    [Fact]
    public async Task GetAsync_WithUserAgent_SendsItOnEveryRequest()
    {
        // Arrange (Given)
        string? agent = null;
        var handler = new StubHandler(request =>
        {
            agent = request.Headers.TryGetValues("User-Agent", out var values) ? string.Join(" ", values) : null;
            return Responses.Ok("x");
        });
        var client = Client(handler, new MyelinOptions { UserAgent = "MyApp/1.0 (+https://example.com)" });

        // Act (When)
        await client.GetAsync(new Uri(Allowed), 1024, Ct);

        // Assert (Then)
        Assert.Equal("MyApp/1.0 (+https://example.com)", agent);
    }

    [Fact]
    public async Task PostJsonAsync_WithAllowedHost_SendsTheBodyAsJson()
    {
        // Arrange (Given)
        string? method = null, contentType = null;
        byte[]? body = null;
        var handler = new StubHandler(r =>
        {
            (method, contentType, body) = (r.Method.Method, r.Content!.Headers.ContentType!.MediaType, r.Content.ReadAsByteArrayAsync().Result);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        // Act (When)
        var response = await Client(handler).PostJsonAsync(new Uri("https://api.example.com/hooks/1/secret"), [1, 2, 3], 1024, Ct);

        // Assert (Then)
        Assert.Equal((HttpStatusCode.NoContent, "POST", "application/json"), (response.Status, method, contentType));
        Assert.Equal(new byte[] { 1, 2, 3 }, body);
    }

    [Fact]
    public async Task PostJsonAsync_WithRedirect_RefusesInsteadOfFollowing()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => Responses.Redirect("https://api.example.com/elsewhere"));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).PostJsonAsync(new Uri(Allowed), [1], 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.RedirectRefused, refusal);
        Assert.Single(handler.Requested);
    }

    [Fact]
    public async Task PostJsonAsync_WithHostNotAllowed_RefusesBeforeSending()
    {
        // Arrange (Given)
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        // Act (When)
        var refusal = await RefusalOf(Client(handler).PostJsonAsync(new Uri("https://evil.example/hooks/1"), [1], 1024, Ct));

        // Assert (Then)
        Assert.Equal(RefusalReasons.HostNotAllowed, refusal);
        Assert.Empty(handler.Requested);
    }

    [Fact]
    public async Task SendOnceAsync_WithHeaders_SendsTheMethodAndHeadersAndRefusesARedirect()
    {
        // Arrange (Given)
        string? method = null, auth = null;
        var ok = new StubHandler(r =>
        {
            (method, auth) = (r.Method.Method, r.Headers.Authorization?.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer t" };

        // Act (When)
        var response = await Client(ok).SendOnceAsync(HttpMethod.Delete, new Uri(Allowed), null, headers, 1024, Ct);
        var refusal = await RefusalOf(Client(new StubHandler(_ => Responses.Redirect("https://api.example.com/e")))
            .SendOnceAsync(HttpMethod.Get, new Uri(Allowed), null, headers, 1024, Ct));

        // Assert (Then)
        Assert.Equal((HttpStatusCode.OK, "DELETE", "Bearer t"), (response.Status, method, auth));
        Assert.Equal(RefusalReasons.RedirectRefused, refusal);
    }

    [Fact]
    public async Task GetAsync_WhenTheTimeoutElapses_ThrowsOperationCanceled()
    {
        // Arrange (Given)
        var handler = new SlowHandler();
        var client = Client(handler, new MyelinOptions { Timeout = TimeSpan.FromMilliseconds(50) });

        // Act & Assert (When / Then)
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(new Uri(Allowed), 1024, Ct));
    }

    [Fact]
    public void Constructor_WithNullArgument_Throws()
    {
        // Act & Assert (When / Then)
        Assert.Throws<ArgumentNullException>(() => new SafeHttpClient(null!, new MyelinOptions(), NullLogger<SafeHttpClient>.Instance));
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
