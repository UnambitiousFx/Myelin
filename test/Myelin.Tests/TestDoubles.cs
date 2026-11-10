using System.Net;

namespace UnambitiousFx.Myelin.Tests;

/// <summary>Answers each request with the next programmed response and records what was asked.</summary>
internal sealed class StubHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _next;

    public List<Uri> Requested { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requested.Add(request.RequestUri!);
        return Task.FromResult(responses[Math.Min(_next++, responses.Length - 1)](request));
    }
}

/// <summary>Resolves every name to fixed answers and counts the lookups.</summary>
internal sealed class FixedResolver(params string[] addresses) : IHostResolver
{
    public List<string> Lookups { get; } = [];

    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        Lookups.Add(host);
        return Task.FromResult(addresses.Select(IPAddress.Parse).ToArray());
    }
}

internal static class Responses
{
    public static HttpResponseMessage Ok(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
    }

    public static HttpResponseMessage Redirect(string location)
    {
        return new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };
    }
}
