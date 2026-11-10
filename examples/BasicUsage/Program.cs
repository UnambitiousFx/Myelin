using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using UnambitiousFx.Myelin;

// Typical SSRF targets: each is refused before any byte leaves the machine, so this sample needs no network.
// CI publishes it with NativeAOT and checks every line.
var client = new SafeHttpClient(
    new HttpMessageInvoker(MyelinHandler.Create()),
    new MyelinOptions { UserAgent = "Myelin-Example/1.0" },
    NullLogger<SafeHttpClient>.Instance);

string[] targets =
[
    "https://169.254.169.254/latest/meta-data/", // cloud metadata
    "https://localhost:8080/admin", // a local service
    "https://[::ffff:10.0.0.1]/", // a private address in IPv6 clothing
    "http://example.com/", // plain HTTP
    "https://user:secret@example.com/" // credentials in the URL
];

foreach (var target in targets)
{
    try
    {
        await client.GetAsync(new Uri(target), 1024);
        Console.WriteLine($"{target} -> sent");
    }
    catch (OutboundRefusedException refused)
    {
        Console.WriteLine($"{target} -> {refused.Reason}");
    }
}

Console.WriteLine($"8.8.8.8 allowed: {AddressPolicy.IsAllowed(IPAddress.Parse("8.8.8.8"))}");
Console.WriteLine($"64:ff9b::a00:1 allowed: {AddressPolicy.IsAllowed(IPAddress.Parse("64:ff9b::a00:1"))}");
