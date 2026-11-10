using System.Net;
using System.Net.Sockets;

namespace UnambitiousFx.Myelin;

/// <summary>
///     The addresses an outbound request may never reach: every special-purpose range of the IANA IPv4 and IPv6
///     registries that is not globally reachable, plus multicast and broadcast. Checked after name resolution, with
///     <see cref="IPNetwork" />, never by parsing text: octal, hexadecimal and mapped notations are what hand-written
///     parsers get wrong.
/// </summary>
/// <remarks>
///     IPv4-mapped IPv6 addresses (<c>::ffff:a.b.c.d</c>) and the well-known NAT64 prefix (<c>64:ff9b::/96</c>) are judged
///     on the IPv4 address they embed. <c>192.0.0.0/24</c> and <c>2001::/23</c> are refused whole, including the few
///     anycast services they hold.
/// </remarks>
public static class AddressPolicy
{
    // The well-known NAT64 prefix: judged on its embedded IPv4 address, never refused wholesale.
    private static readonly IPNetwork Nat64WellKnown = IPNetwork.Parse("64:ff9b::/96");

    private static readonly IPNetwork[] RefusedV4 =
    [
        IPNetwork.Parse("0.0.0.0/8"), // "this network"
        IPNetwork.Parse("10.0.0.0/8"), // private
        IPNetwork.Parse("100.64.0.0/10"), // carrier-grade NAT
        IPNetwork.Parse("127.0.0.0/8"), // loopback
        IPNetwork.Parse("169.254.0.0/16"), // link-local, cloud metadata endpoints
        IPNetwork.Parse("172.16.0.0/12"), // private
        IPNetwork.Parse("192.0.0.0/24"), // IETF protocol assignments
        IPNetwork.Parse("192.0.2.0/24"), // documentation (TEST-NET-1)
        IPNetwork.Parse("192.88.99.0/24"), // deprecated 6to4 relay anycast
        IPNetwork.Parse("192.168.0.0/16"), // private
        IPNetwork.Parse("198.18.0.0/15"), // benchmarking
        IPNetwork.Parse("198.51.100.0/24"), // documentation (TEST-NET-2)
        IPNetwork.Parse("203.0.113.0/24"), // documentation (TEST-NET-3)
        IPNetwork.Parse("224.0.0.0/4"), // multicast
        IPNetwork.Parse("240.0.0.0/4") // reserved, and 255.255.255.255 broadcast
    ];

    private static readonly IPNetwork[] RefusedV6 =
    [
        IPNetwork.Parse("::/96"), // IPv4-compatible (embeds IPv4), unspecified ::, loopback ::1
        IPNetwork.Parse("::ffff:0:0:0/96"), // SIIT IPv4-translated
        IPNetwork.Parse("64:ff9b:1::/48"), // local-use NAT64
        IPNetwork.Parse("100::/64"), // discard-only
        IPNetwork.Parse("100:0:0:1::/64"), // dummy prefix (RFC 9780)
        IPNetwork.Parse("2001::/23"), // IETF protocol assignments: Teredo, benchmarking, ORCHID...
        IPNetwork.Parse("2001:db8::/32"), // documentation
        IPNetwork.Parse("2002::/16"), // 6to4 (embeds IPv4)
        IPNetwork.Parse("3fff::/20"), // documentation
        IPNetwork.Parse("5f00::/16"), // segment routing (SRv6) SIDs
        IPNetwork.Parse("fc00::/7"), // unique local
        IPNetwork.Parse("fe80::/10"), // link-local
        IPNetwork.Parse("fec0::/10"), // deprecated site-local
        IPNetwork.Parse("ff00::/8") // multicast
    ];

    /// <summary>Tells whether an outbound request may connect to an address.</summary>
    /// <param name="address">A resolved address.</param>
    /// <returns><see langword="true" /> for a globally reachable unicast address; <see langword="false" /> otherwise.</returns>
    public static bool IsAllowed(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && Nat64WellKnown.Contains(address))
        {
            return IsAllowed(new IPAddress(address.GetAddressBytes()[12..]));
        }

        var refused = address.AddressFamily == AddressFamily.InterNetwork ? RefusedV4 : RefusedV6;
        foreach (var network in refused)
        {
            if (network.Contains(address))
            {
                return false;
            }
        }

        return true;
    }
}
