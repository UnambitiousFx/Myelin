using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace UnambitiousFx.Myelin;

/// <summary>A response read in full, within the size limit of the call.</summary>
/// <param name="Status">The HTTP status; a 4xx or 5xx is returned, not thrown.</param>
/// <param name="Body">The body bytes.</param>
public sealed record OutboundResponse(HttpStatusCode Status, byte[] Body);

/// <summary>
///     An outbound HTTP client for URLs you do not fully control: HTTPS only, optional host allow-list, public addresses
///     only (checked after resolution, DNS rebinding included), every redirect revalidated, bounded time and size, each
///     refusal logged with its host and reason only.
/// </summary>
/// <remarks>Register it with <c>services.AddMyelin()</c>; build one by hand with <see cref="MyelinHandler.Create" /> in tests.</remarks>
public sealed partial class SafeHttpClient
{
    private readonly HttpMessageInvoker _invoker;
    private readonly ILogger<SafeHttpClient> _logger;
    private readonly MyelinOptions _options;

    /// <summary>Creates a client over an invoker, normally one wrapping <see cref="MyelinHandler.Create" />.</summary>
    /// <param name="invoker">Sends the requests; the address checks live in its handler.</param>
    /// <param name="options">The allow-list, User-Agent, timeout and redirect limit.</param>
    /// <param name="logger">Receives one warning per refusal.</param>
    public SafeHttpClient(HttpMessageInvoker invoker, MyelinOptions options, ILogger<SafeHttpClient> logger)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _invoker = invoker;
        _options = options;
        _logger = logger;
    }

    /// <summary>GETs a URL, following up to <see cref="MyelinOptions.MaxRedirects" /> redirects, each checked like the first request.</summary>
    /// <param name="url">An absolute <c>https</c> URL.</param>
    /// <param name="maxBytes">The largest body accepted, declared or streamed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The final response, whatever its status.</returns>
    /// <exception cref="OutboundRefusedException">The request or its response was refused; see <see cref="OutboundRefusedException.Reason" />.</exception>
    public async Task<OutboundResponse> GetAsync(Uri url, int maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        var current = url;
        for (var hop = 0;; hop++)
        {
            Check(current);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            AddUserAgent(request);
            using var response = await SendAsync(request, timeout.Token).ConfigureAwait(false);

            if (IsRedirect(response.StatusCode))
            {
                if (hop >= _options.MaxRedirects)
                {
                    throw Refuse(RefusalReasons.TooManyRedirects, current.Host);
                }

                if (response.Headers.Location is not { } location)
                {
                    throw Refuse(RefusalReasons.RedirectWithoutLocation, current.Host);
                }

                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                continue;
            }

            return await ReadAsync(response, maxBytes, current.Host, timeout.Token).ConfigureAwait(false);
        }
    }

    /// <summary>POSTs a JSON body once. A redirect is refused, never followed: a POST must not be replayed elsewhere, and a webhook URL is a secret.</summary>
    /// <param name="url">An absolute <c>https</c> URL.</param>
    /// <param name="json">The UTF-8 JSON body, sent as <c>application/json</c>.</param>
    /// <param name="maxBytes">The largest response body accepted.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The response, whatever its status.</returns>
    /// <exception cref="OutboundRefusedException">The request or its response was refused.</exception>
    public Task<OutboundResponse> PostJsonAsync(Uri url, byte[] json, int maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        return SendOnceAsync(HttpMethod.Post, url, json, null, maxBytes, cancellationToken);
    }

    /// <summary>
    ///     Sends one request with any method, extra headers (a bearer token) and an optional JSON body. A redirect is
    ///     refused, never followed: the headers are meant for this host only.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">An absolute <c>https</c> URL.</param>
    /// <param name="json">The UTF-8 JSON body, or <see langword="null" /> for none.</param>
    /// <param name="headers">Extra request headers, or <see langword="null" />.</param>
    /// <param name="maxBytes">The largest response body accepted.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The response, whatever its status.</returns>
    /// <exception cref="OutboundRefusedException">The request or its response was refused.</exception>
    public async Task<OutboundResponse> SendOnceAsync(
        HttpMethod method,
        Uri url,
        byte[]? json,
        IReadOnlyDictionary<string, string>? headers,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        Check(url);
        using var request = new HttpRequestMessage(method, url);
        if (json is not null)
        {
            request.Content = new ByteArrayContent(json);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        AddUserAgent(request);
        if (headers is not null)
        {
            foreach (var (name, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }

        using var response = await SendAsync(request, timeout.Token).ConfigureAwait(false);
        if (IsRedirect(response.StatusCode))
        {
            throw Refuse(RefusalReasons.RedirectRefused, url.Host);
        }

        return await ReadAsync(response, maxBytes, url.Host, timeout.Token).ConfigureAwait(false);
    }

    private void Check(Uri url)
    {
        if (UrlPolicy.RefusalOf(url, _options) is { } reason)
        {
            throw Refuse(reason, UrlPolicy.HostOf(url));
        }
    }

    private void AddUserAgent(HttpRequestMessage request)
    {
        if (_options.UserAgent is { Length: > 0 } agent)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", agent);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e) when (RefusalIn(e) is { } refused)
        {
            throw Refuse(refused.Reason, refused.Host);
        }
    }

    private async Task<OutboundResponse> ReadAsync(HttpResponseMessage response, int maxBytes, string host, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw Refuse(RefusalReasons.TooLarge, host);
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                // The declared length can be absent or wrong: the limit holds on what is actually read.
                if (buffer.Length + read > maxBytes)
                {
                    throw Refuse(RefusalReasons.TooLarge, host);
                }

                buffer.Write(chunk, 0, read);
            }

            return new OutboundResponse(response.StatusCode, buffer.ToArray());
        }
    }

    internal static OutboundRefusedException? RefusalIn(Exception e)
    {
        for (var inner = e; inner is not null; inner = inner.InnerException)
        {
            if (inner is OutboundRefusedException refused)
            {
                return refused;
            }
        }

        return null;
    }

    private static bool IsRedirect(HttpStatusCode status)
    {
        return status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    }

    private OutboundRefusedException Refuse(string reason, string host)
    {
        LogRefused(host, reason);
        return new OutboundRefusedException(reason, host);
    }

    // Host and reason only: a path or query string could carry a secret (a webhook token).
    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbound request to {Host} refused: {Reason}")]
    private partial void LogRefused(string host, string reason);
}
