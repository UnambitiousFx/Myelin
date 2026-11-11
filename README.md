# UnambitiousFx.Myelin

[![Build Status](https://github.com/UnambitiousFx/Myelin/workflows/CI/badge.svg)](https://github.com/UnambitiousFx/Myelin/actions)
[![NuGet](https://img.shields.io/nuget/v/UnambitiousFx.Myelin.svg)](https://www.nuget.org/packages/UnambitiousFx.Myelin/)
[![codecov](https://codecov.io/gh/UnambitiousFx/Myelin/branch/main/graph/badge.svg)](https://codecov.io/gh/UnambitiousFx/Myelin)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Documentation](https://img.shields.io/badge/docs-myelin.unambitiousfx.com-blue)](https://myelin.unambitiousfx.com)

SSRF-safe outbound HTTP for .NET. Use it whenever your server fetches a URL it did not write itself: webhooks,
« import from URL », link previews, avatar or icon fetchers, third-party callbacks.

A plain `HttpClient` connects to `127.0.0.1`, to the cloud metadata endpoint `169.254.169.254` or to your private
network if the URL says so. Checking the URL text is not enough: `0x7f000001` is `127.0.0.1`, a public name can resolve
to a private address (or change its answer between your check and the connection), and a harmless URL can redirect to a
harmful one. Myelin closes those doors.

> *Myelin* is the sheath around a nerve fibre that keeps the signal from leaking. Here, it keeps your server's requests
> from leaking into your own network.

## 🔧 Compatibility & support

- **Dependencies**: `Microsoft.Extensions.Http`, `Logging.Abstractions` and `Options` only.
- **NativeAOT and trimming compatible**: CI publishes and runs an AOT binary on every change.
- **Supported .NET versions**: `net8.0`, `net9.0`, `net10.0`, and `net11.0`.

## 🎯 Features

- **Public addresses only**: every special-purpose range of the IANA IPv4 and IPv6 registries is refused (private,
  loopback, link-local and cloud metadata, carrier-grade NAT, documentation, multicast…), IPv4-mapped and NAT64 forms
  judged on the IPv4 address they embed.
- **DNS rebinding proof**: the host is resolved once, every answer checked, and the socket connects to exactly those
  addresses. No proxy.
- **HTTPS only**, no credentials in the URL, optional **host allow-list** with `*.example.com` wildcards.
- **Every redirect revalidated** for a GET; a POST or a request with extra headers is never redirected.
- **Bounded time and size**, declared or streamed.
- **Quiet logs**: a refusal is logged with its host and reason, never a path that may hold a secret.
- **`IHttpClientFactory` integration**: `AddHttpClient<T>().UseMyelin()`.

## 📦 Installation

```bash
dotnet add package UnambitiousFx.Myelin
```

## 🚀 Quick start

```csharp
using UnambitiousFx.Myelin;

builder.Services.AddMyelin(o =>
{
    o.AllowedHosts = ["api.example.com", "*.cdn.example.org"]; // optional: empty allows any public host
    o.UserAgent = "MyApp/1.0";
});

public sealed class IconFetcher(SafeHttpClient http)
{
    public async Task<byte[]?> FetchAsync(Uri url, CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync(url, maxBytes: 512_000, ct);
            return response.Status == HttpStatusCode.OK ? response.Body : null;
        }
        catch (OutboundRefusedException refused) // refused.Reason: "address_refused", "not_https", "too_large"...
        {
            return null;
        }
    }
}
```

Already using `IHttpClientFactory`?

```csharp
builder.Services.AddHttpClient<WebhookSender>().UseMyelin(o => o.AllowedHosts = ["hooks.example.com"]);
```

## 📚 API

| Type | Role |
|---|---|
| `SafeHttpClient` | `GetAsync` (redirects revalidated), `PostJsonAsync`, `SendOnceAsync`; bodies read within `maxBytes`. |
| `MyelinHandler.Create(resolver?)` | The `SocketsHttpHandler` that resolves, checks and connects. |
| `AddressPolicy.IsAllowed(IPAddress)` | The address check on its own. |
| `MyelinOptions` | `AllowedHosts`, `UserAgent`, `Timeout`, `MaxRedirects`. |
| `OutboundRefusedException` | `Reason` (see `RefusalReasons`) and `Host`. |
| `IHostResolver` | Replace DNS resolution (tests, custom resolvers). |

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## 📄 License

[MIT](LICENSE).
