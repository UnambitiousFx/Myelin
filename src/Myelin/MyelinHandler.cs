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
        var hosts = resolver ?? new DnsHostResolver();
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var addresses = await hosts.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
                if (addresses.Length == 0 || !addresses.All(AddressPolicy.IsAllowed))
                {
                    throw new OutboundRefusedException(RefusalReasons.AddressRefused, host);
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }
}
