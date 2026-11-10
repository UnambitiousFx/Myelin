using System.Net;

namespace UnambitiousFx.Myelin;

/// <summary>Resolves a host name to the addresses the connection may use. Replace it in tests, or to use another resolver.</summary>
public interface IHostResolver
{
    /// <summary>Resolves a host name, or parses an address literal.</summary>
    /// <param name="host">The host of the request URL.</param>
    /// <param name="cancellationToken">Cancels the resolution.</param>
    /// <returns>Every address the name resolves to.</returns>
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}

/// <summary>Resolves through the operating system, as <see cref="Dns.GetHostAddressesAsync(string, CancellationToken)" /> does.</summary>
public sealed class DnsHostResolver : IHostResolver
{
    /// <inheritdoc />
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        return Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}
