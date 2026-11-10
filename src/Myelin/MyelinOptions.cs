namespace UnambitiousFx.Myelin;

/// <summary>What an outbound request may reach, and how long and how much it may take.</summary>
public sealed class MyelinOptions
{
    /// <summary>
    ///     The hosts a request may target. Empty (the default) allows any host whose addresses are public. An entry is a
    ///     host name (<c>api.example.com</c>) or a wildcard for its subdomains (<c>*.example.com</c>, which does not match
    ///     <c>example.com</c> itself); comparison ignores case and uses the IDN (punycode) form.
    /// </summary>
    /// <remarks>An array, not a list: configuration binding replaces an array but appends to a pre-filled list.</remarks>
    public string[] AllowedHosts { get; set; } = [];

    /// <summary>The <c>User-Agent</c> sent on every request; some servers refuse a request without one. <see langword="null" /> sends none.</summary>
    public string? UserAgent { get; set; }

    /// <summary>The time a whole call may take, redirects and body included. Default: 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How many redirects a GET may follow. Default: 3.</summary>
    public int MaxRedirects { get; set; } = 3;
}
