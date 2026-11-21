using System.Net;
using System.Net.Sockets;

namespace UnambitiousFx.Myelin;

/// <summary>Creates the connection handler at the bottom of every Myelin client.</summary>
public static class MyelinHandler
{
    /// <summary>
    ///     Creates a <see cref="SocketsHttpHandler" /> that resolves each host itself, refuses it when any answer is an
    ///     address <see cref="AddressPolicy" /> refuses, and connects only to the addresses it checked, so a second,
    ///     different DNS answer (DNS rebinding) is never what the connection uses.
    /// </summary>
    /// <param name="resolver">The resolver; <see cref="DnsHostResolver" /> when <see langword="null" />.</param>
    /// <returns>
    ///     A handler with automatic redirects, cookies and proxies off: a redirect must be checked like the first request,
    ///     and a proxy would make the connection to an address this check never sees.
    /// </returns>
    /// <remarks>A refused connection surfaces as an <see cref="HttpRequestException" /> wrapping an <see cref="OutboundRefusedException" />.</remarks>
    public static SocketsHttpHandler Create(IHostResolver? resolver = null)
    {
        return Create(resolver, null);
    }

    /// <summary>
    ///     Creates the handler of <see cref="Create(IHostResolver?)" />, letting the registered endpoints also reach
    ///     private-use, shared (CGNAT) and unique-local addresses (<see cref="AddressPolicy.IsAllowedForRegisteredEndpoint" />).
    /// </summary>
    /// <param name="resolver">The resolver; <see cref="DnsHostResolver" /> when <see langword="null" />.</param>
    /// <param name="registeredEndpoints">
    ///     The exact <c>https</c> host and port pairs exempted, asked on every new connection that needs the exemption;
    ///     <see langword="null" /> exempts none.
    /// </param>
    /// <returns>
    ///     The handler. With registered endpoints, pooled connections live one minute at most, so a revoked endpoint stops
    ///     being reached within that time.
    /// </returns>
    /// <remarks>A refused connection surfaces as an <see cref="HttpRequestException" /> wrapping an <see cref="OutboundRefusedException" />.</remarks>
    public static SocketsHttpHandler Create(IHostResolver? resolver, IRegisteredEndpoints? registeredEndpoints)
    {
        var hosts = resolver ?? new DnsHostResolver();
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var port = context.DnsEndPoint.Port;
                var addresses = await hosts.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
                if (!await IsPermittedAsync(host, port, context.InitialRequestMessage.RequestUri, addresses, registeredEndpoints, cancellationToken)
                        .ConfigureAwait(false))
                {
                    throw new OutboundRefusedException(RefusalReasons.AddressRefused, host);
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, port, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        if (registeredEndpoints is not null)
        {
            // The exemption is decided per connection: bound how long a pooled one outlives a revocation.
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(1);
        }

        return handler;
    }

    /// <summary>Whether a connection may use every resolved address: public ones, or private ones for a registered https endpoint.</summary>
    internal static async ValueTask<bool> IsPermittedAsync(
        string host,
        int port,
        Uri? requestUri,
        IPAddress[] addresses,
        IRegisteredEndpoints? registeredEndpoints,
        CancellationToken cancellationToken)
    {
        if (addresses.Length == 0)
        {
            return false;
        }

        if (addresses.All(AddressPolicy.IsAllowed))
        {
            return true;
        }

        return registeredEndpoints is not null
               && requestUri is { IsAbsoluteUri: true } && requestUri.Scheme == Uri.UriSchemeHttps
               && addresses.All(AddressPolicy.IsAllowedForRegisteredEndpoint)
               && await registeredEndpoints.IsRegisteredAsync(host, port, cancellationToken).ConfigureAwait(false);
    }
}
