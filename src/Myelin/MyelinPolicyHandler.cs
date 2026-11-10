using Microsoft.Extensions.Options;

namespace UnambitiousFx.Myelin;

/// <summary>
///     Checks every request of an <see cref="HttpClient" /> built by <c>IHttpClientFactory</c> with <c>UseMyelin()</c>:
///     HTTPS, no credentials in the URL, the allow-list. The address checks live in the primary handler
///     (<see cref="MyelinHandler.Create" />), which follows no redirect: a 3xx reaches the caller as it is.
/// </summary>
internal sealed class MyelinPolicyHandler(IOptionsMonitor<MyelinOptions> options, string name) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri ?? throw new OutboundRefusedException(RefusalReasons.NotHttps, "");
        var current = options.Get(name);
        if (UrlPolicy.RefusalOf(url, current) is { } reason)
        {
            throw new OutboundRefusedException(reason, UrlPolicy.HostOf(url));
        }

        if (current.UserAgent is { Length: > 0 } agent && !request.Headers.UserAgent.Any())
        {
            request.Headers.TryAddWithoutValidation("User-Agent", agent);
        }

        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e) when (SafeHttpClient.RefusalIn(e) is { } refused)
        {
            // Surface the refusal itself rather than the transport exception that wraps it.
            throw refused;
        }
    }
}
