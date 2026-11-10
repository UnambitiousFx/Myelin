---
name: unambitiousfx-myelin
description: Make outbound HTTP calls to URLs the application does not fully control (webhooks, import from URL, link previews, icon or avatar fetchers, third-party callbacks) without opening an SSRF hole, with UnambitiousFx.Myelin. Use when a .NET project references UnambitiousFx.Myelin, or when code fetches a user-supplied or configurable URL.
---

# UnambitiousFx.Myelin

SSRF-safe outbound HTTP. Namespace: `UnambitiousFx.Myelin`.

## Rules

- Never fetch a user-supplied or configurable URL with a plain `HttpClient`. Use `SafeHttpClient`, or a client
  registered with `.UseMyelin()`.
- Never validate a URL by its text (`url.Host != "localhost"`): Myelin checks the resolved addresses, which is the only
  check that holds against encodings, DNS rebinding and redirects.
- Catch `OutboundRefusedException` where the URL enters the system; its `Reason` is one of `RefusalReasons`. A 4xx or
  5xx is not an exception: read `OutboundResponse.Status`.
- Always pass a `maxBytes` that fits the use (an icon: a few hundred KB; a webhook answer: a few KB).
- Do not log the URL of a refused request: it may carry a token. Myelin already logs host and reason.

## Register

```csharp
builder.Services.AddMyelin(o =>
{
    o.AllowedHosts = ["api.example.com", "*.cdn.example.org"]; // empty = any public host
    o.UserAgent = "MyApp/1.0";
});
```

## Use

```csharp
public sealed class Fetcher(SafeHttpClient http)
{
    public Task<OutboundResponse> GetAsync(Uri url, CancellationToken ct) => http.GetAsync(url, 512_000, ct);       // redirects revalidated
    public Task<OutboundResponse> NotifyAsync(Uri hook, byte[] json, CancellationToken ct) => http.PostJsonAsync(hook, json, 4096, ct); // never redirected
}
```

`SendOnceAsync(method, url, json, headers, maxBytes, ct)` sends any method with extra headers (a bearer token); it is
never redirected either.

## IHttpClientFactory

```csharp
builder.Services.AddHttpClient<WebhookSender>().UseMyelin(o => o.AllowedHosts = ["hooks.example.com"]);
```

The client refuses unsafe URLs and addresses, and returns redirects instead of following them.

## Tests

Register a fake `IHostResolver` before `AddMyelin()` to control DNS answers; build a `SafeHttpClient` over a stub
`HttpMessageHandler` to control responses. Never disable the checks in tests.
