namespace UnambitiousFx.Myelin;

/// <summary>
///     The endpoints an application registered itself (a worker on its own private network) that may resolve to a
///     private-use, shared (CGNAT) or unique-local address. Opt in with
///     <see cref="MyelinOptions.AllowRegisteredEndpoints" />; everything else keeps the default policy.
/// </summary>
/// <remarks>
///     Asked on every new connection that resolves to such an address, so registering or revoking an endpoint applies
///     without rebuilding the client. Loopback, link-local, cloud metadata, multicast and every other refused range stay
///     refused even for a registered endpoint. Called concurrently from a long-lived handler: keep it thread-safe and do not
///     capture a scoped service.
/// </remarks>
public interface IRegisteredEndpoints
{
    /// <summary>Tells whether an exact host and port pair is registered. No wildcard: match both exactly.</summary>
    /// <param name="host">The host of the request URL, in its IDN (punycode) form, as the URL gives it.</param>
    /// <param name="port">The port of the request URL (443 when the URL has none).</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns><see langword="true" /> when the pair is registered.</returns>
    ValueTask<bool> IsRegisteredAsync(string host, int port, CancellationToken cancellationToken);
}
