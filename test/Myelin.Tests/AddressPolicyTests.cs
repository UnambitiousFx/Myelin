using System.Net;

namespace UnambitiousFx.Myelin.Tests;

public class AddressPolicyTests
{
    // One address from every entry of the IANA IPv4 and IPv6 special-purpose address registries
    // (https://www.iana.org/assignments/iana-ipv4-special-registry, iana-ipv6-special-registry) that is not
    // globally reachable, plus multicast, broadcast and the deprecated site-local range.
    [Theory]
    [InlineData("0.0.0.0")] // 0.0.0.0/8 "this network"
    [InlineData("0.255.255.255")]
    [InlineData("10.0.0.0")] // 10.0.0.0/8 private
    [InlineData("10.255.255.255")]
    [InlineData("100.64.0.1")] // 100.64.0.0/10 shared address space
    [InlineData("100.127.255.255")]
    [InlineData("127.0.0.1")] // 127.0.0.0/8 loopback
    [InlineData("127.255.255.254")]
    [InlineData("169.254.169.254")] // 169.254.0.0/16 link-local, cloud metadata
    [InlineData("172.16.0.1")] // 172.16.0.0/12 private
    [InlineData("172.31.255.255")]
    [InlineData("192.0.0.1")] // 192.0.0.0/24 IETF protocol assignments
    [InlineData("192.0.0.9")] // 192.0.0.9/32 PCP anycast: refused with its /24
    [InlineData("192.0.0.170")] // 192.0.0.170/32 NAT64/DNS64 discovery
    [InlineData("192.0.2.1")] // 192.0.2.0/24 documentation
    [InlineData("192.88.99.1")] // 192.88.99.0/24 deprecated 6to4 relay
    [InlineData("192.168.1.1")] // 192.168.0.0/16 private
    [InlineData("198.18.0.1")] // 198.18.0.0/15 benchmarking
    [InlineData("198.19.255.255")]
    [InlineData("198.51.100.1")] // 198.51.100.0/24 documentation
    [InlineData("203.0.113.1")] // 203.0.113.0/24 documentation
    [InlineData("224.0.0.1")] // 224.0.0.0/4 multicast
    [InlineData("239.255.255.255")]
    [InlineData("240.0.0.1")] // 240.0.0.0/4 reserved
    [InlineData("255.255.255.255")] // limited broadcast
    [InlineData("::")] // ::/128 unspecified
    [InlineData("::1")] // ::1/128 loopback
    [InlineData("::7f00:1")] // ::/96 IPv4-compatible, embeds 127.0.0.1
    [InlineData("::a9fe:a9fe")] // ::/96 IPv4-compatible, embeds 169.254.169.254
    [InlineData("::ffff:10.0.0.1")] // ::ffff:0:0/96 IPv4-mapped, judged on 10.0.0.1
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:0:a00:1")] // ::ffff:0:0:0/96 SIIT, embeds 10.0.0.1
    [InlineData("64:ff9b::a00:1")] // 64:ff9b::/96 well-known NAT64, judged on 10.0.0.1
    [InlineData("64:ff9b::7f00:1")] // ... on 127.0.0.1
    [InlineData("64:ff9b:1::a00:1")] // 64:ff9b:1::/48 local-use NAT64
    [InlineData("100::1")] // 100::/64 discard-only
    [InlineData("100:0:0:1::1")] // 100:0:0:1::/64 dummy prefix
    [InlineData("2001::1")] // 2001::/32 Teredo
    [InlineData("2001:1::1")] // 2001:1::1/128 PCP anycast: refused with 2001::/23
    [InlineData("2001:2::1")] // 2001:2::/48 benchmarking
    [InlineData("2001:10::1")] // 2001:10::/28 deprecated ORCHID
    [InlineData("2001:20::1")] // 2001:20::/28 ORCHIDv2
    [InlineData("2001:1ff:ffff::1")] // end of 2001::/23
    [InlineData("2001:db8::1")] // 2001:db8::/32 documentation
    [InlineData("2002::1")] // 2002::/16 6to4
    [InlineData("2002:a00:1::1")] // 6to4 embedding 10.0.0.1
    [InlineData("3fff::1")] // 3fff::/20 documentation
    [InlineData("5f00::1")] // 5f00::/16 segment routing SIDs
    [InlineData("fc00::1")] // fc00::/7 unique local
    [InlineData("fd12:3456::1")]
    [InlineData("fe80::1")] // fe80::/10 link-local
    [InlineData("febf:ffff::1")]
    [InlineData("fec0::1")] // fec0::/10 deprecated site-local
    [InlineData("ff02::1")] // ff00::/8 multicast
    public void IsAllowed_WithSpecialPurposeAddress_ReturnsFalse(string address)
    {
        // Arrange (Given)
        var ip = IPAddress.Parse(address);

        // Act (When)
        var allowed = AddressPolicy.IsAllowed(ip);

        // Assert (Then)
        Assert.False(allowed);
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("185.199.108.133")]
    [InlineData("1.1.1.1")]
    [InlineData("9.255.255.255")] // just below 10.0.0.0/8
    [InlineData("11.0.0.0")] // just above
    [InlineData("100.63.255.255")] // just below 100.64.0.0/10
    [InlineData("100.128.0.0")] // just above
    [InlineData("172.15.255.255")] // just below 172.16.0.0/12
    [InlineData("172.32.0.1")] // just above
    [InlineData("192.0.1.1")] // between 192.0.0.0/24 and 192.0.2.0/24
    [InlineData("192.31.196.1")] // AS112-v4: globally reachable per IANA
    [InlineData("192.52.193.1")] // AMT: globally reachable per IANA
    [InlineData("192.175.48.1")] // direct delegation AS112: globally reachable per IANA
    [InlineData("223.255.255.255")] // just below multicast
    [InlineData("2606:50c0:8000::154")]
    [InlineData("2a00:1450:4007:80f::200e")]
    [InlineData("2001:200::1")] // just above 2001::/23
    [InlineData("2620:4f:8000::1")] // direct delegation AS112: globally reachable per IANA
    [InlineData("::ffff:8.8.8.8")] // IPv4-mapped public
    [InlineData("64:ff9b::808:808")] // well-known NAT64 embedding 8.8.8.8
    public void IsAllowed_WithGloballyReachableAddress_ReturnsTrue(string address)
    {
        // Arrange (Given)
        var ip = IPAddress.Parse(address);

        // Act (When)
        var allowed = AddressPolicy.IsAllowed(ip);

        // Assert (Then)
        Assert.True(allowed);
    }

    [Fact]
    public void IsAllowed_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert (When / Then)
        Assert.Throws<ArgumentNullException>(() => AddressPolicy.IsAllowed(null!));
    }
}
