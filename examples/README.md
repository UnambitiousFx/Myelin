# Examples

| Example | What it shows |
|---|---|
| [`BasicUsage`](BasicUsage) | Typical SSRF targets (cloud metadata, localhost, a private address in IPv6 form, plain HTTP, credentials in the URL), each refused before any byte leaves the machine. Needs no network. CI publishes it with NativeAOT. |

```bash
dotnet run --project examples/BasicUsage --framework net11.0
```
