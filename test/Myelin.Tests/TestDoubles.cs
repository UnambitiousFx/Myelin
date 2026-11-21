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

/// <summary>A registered-endpoint set that can change between requests, and records each question.</summary>
internal sealed class RegisteredSet(params (string Host, int Port)[] endpoints) : IRegisteredEndpoints
{
    public HashSet<(string Host, int Port)> Endpoints { get; } = [..endpoints];

    public List<(string Host, int Port)> Asked { get; } = [];

    public ValueTask<bool> IsRegisteredAsync(string host, int port, CancellationToken cancellationToken)
    {
        Asked.Add((host, port));
        return ValueTask.FromResult(Endpoints.Contains((host, port)));
    }
}

/// <summary>Answers requests to one host itself and sends every other request to the inner handler.</summary>
internal sealed class OneHostHandler(string host, Func<HttpRequestMessage, HttpResponseMessage> respond, HttpMessageHandler inner)
    : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri!.Host == host ? Task.FromResult(respond(request)) : base.SendAsync(request, cancellationToken);
    }
}
