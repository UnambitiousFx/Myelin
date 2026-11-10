namespace UnambitiousFx.Myelin;

/// <summary>An outbound request Myelin refused to send, or whose response it refused to read.</summary>
/// <param name="reason">Why: one of <see cref="RefusalReasons" />.</param>
/// <param name="host">The host of the refused request; never the path or query, which may hold secrets.</param>
public sealed class OutboundRefusedException(string reason, string host)
    : Exception($"Outbound request to '{host}' refused: {reason}.")
{
    /// <summary>Why the request was refused: one of <see cref="RefusalReasons" />.</summary>
    public string Reason { get; } = reason;

    /// <summary>The host of the refused request.</summary>
    public string Host { get; } = host;
}

/// <summary>The values of <see cref="OutboundRefusedException.Reason" />.</summary>
public static class RefusalReasons
{
    /// <summary>The URL is relative or not <c>https</c>.</summary>
    public const string NotHttps = "not_https";

    /// <summary>The URL carries credentials (<c>https://user:password@host</c>).</summary>
    public const string UserInfo = "user_info";

    /// <summary>The host is not in <see cref="MyelinOptions.AllowedHosts" />.</summary>
    public const string HostNotAllowed = "host_not_allowed";

    /// <summary>The host resolves to no address, or to at least one address <see cref="AddressPolicy" /> refuses.</summary>
    public const string AddressRefused = "address_refused";

    /// <summary>The redirect chain is longer than <see cref="MyelinOptions.MaxRedirects" />.</summary>
    public const string TooManyRedirects = "too_many_redirects";

    /// <summary>A redirect response has no <c>Location</c> header.</summary>
    public const string RedirectWithoutLocation = "redirect_without_location";

    /// <summary>A redirect answered a request that is never redirected (any method but GET without extra headers).</summary>
    public const string RedirectRefused = "redirect_refused";

    /// <summary>The response body is larger than the limit of the call.</summary>
    public const string TooLarge = "too_large";
}
