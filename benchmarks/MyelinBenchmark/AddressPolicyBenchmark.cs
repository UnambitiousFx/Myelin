using System.Net;
using BenchmarkDotNet.Attributes;
using UnambitiousFx.Myelin;

namespace UnambitiousFx.Benchmarks.MyelinBenchmark;

/// <summary>The check runs once per resolved address of every new connection.</summary>
[MemoryDiagnoser]
public class AddressPolicyBenchmark
{
    private readonly IPAddress _publicV4 = IPAddress.Parse("8.8.8.8");
    private readonly IPAddress _privateV4 = IPAddress.Parse("10.0.0.1");
    private readonly IPAddress _publicV6 = IPAddress.Parse("2606:50c0:8000::154");
    private readonly IPAddress _mappedV4 = IPAddress.Parse("::ffff:8.8.8.8");
    private readonly IPAddress _nat64 = IPAddress.Parse("64:ff9b::808:808");

    [Benchmark(Baseline = true)]
    public bool PublicV4()
    {
        return AddressPolicy.IsAllowed(_publicV4);
    }

    [Benchmark]
    public bool PrivateV4()
    {
        return AddressPolicy.IsAllowed(_privateV4);
    }

    [Benchmark]
    public bool PublicV6()
    {
        return AddressPolicy.IsAllowed(_publicV6);
    }

    [Benchmark]
    public bool MappedV4()
    {
        return AddressPolicy.IsAllowed(_mappedV4);
    }

    [Benchmark]
    public bool Nat64()
    {
        return AddressPolicy.IsAllowed(_nat64);
    }
}
