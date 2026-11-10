namespace UnambitiousFx.Myelin;

/// <summary>The checks on a URL itself, before any connection: scheme, credentials, allow-list.</summary>
internal static class UrlPolicy
{
    public static string? RefusalOf(Uri url, MyelinOptions options)
    {
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps)
        {
            return RefusalReasons.NotHttps;
        }

        if (!string.IsNullOrEmpty(url.UserInfo))
        {
            return RefusalReasons.UserInfo;
        }

        return IsHostAllowed(url.IdnHost, options.AllowedHosts) ? null : RefusalReasons.HostNotAllowed;
    }

    public static bool IsHostAllowed(string host, string[] allowedHosts)
    {
        if (allowedHosts.Length == 0)
        {
            return true;
        }

        foreach (var entry in allowedHosts)
        {
            if (entry.StartsWith("*.", StringComparison.Ordinal))
            {
                // "*.example.com" matches "a.example.com" and "a.b.example.com", not "example.com" nor "badexample.com".
                if (host.EndsWith(entry[1..], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(host, entry, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string HostOf(Uri? url)
    {
        return url is { IsAbsoluteUri: true } ? url.Host : "";
    }
}
