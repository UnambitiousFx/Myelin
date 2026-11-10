# UnambitiousFx.Myelin — Guidelines

SSRF-safe outbound HTTP for .NET: public addresses only (checked after resolution, DNS rebinding included), every
redirect revalidated, bounded time and size. Namespace: `UnambitiousFx.Myelin`. Solution: `Myelin.slnx`.

## Layout

| Path | Purpose |
|------|---------|
| `src/Myelin` | The library — `AddressPolicy`, `MyelinHandler`, `SafeHttpClient`, `MyelinOptions`, `AddMyelin` / `UseMyelin` |
| `test/Myelin.Tests` | Tests; `StubHandler` and `FixedResolver` (`TestDoubles.cs`) keep them off the network |
| `benchmarks/MyelinBenchmark` | BenchmarkDotNet benchmarks |
| `examples/BasicUsage` | Console sample; CI publishes it with NativeAOT and checks every refusal it prints |
| `docs/` | Docusaurus site (`myelin.unambitiousfx.com`) |

## Principles

- **Secure by default**: never add an option that weakens the address checks, proxies or redirect revalidation. A
  looser behaviour is the caller's explicit choice (`AllowedHosts` empty), never the default.
- **Check what is connected to, not what is written**: addresses are judged after resolution with `IPNetwork`, never by
  parsing URL text; the socket connects only to the addresses that were checked.
- **The address policy follows the IANA special-purpose registries**: a change cites the registry entry.
- **Quiet**: log host and reason only, never a path or query.
- **NativeAOT-safe**, dependencies limited to `Microsoft.Extensions.Http`, `Logging.Abstractions`, `Options`.

## Conventions

- File-scoped namespaces (`namespace UnambitiousFx.Myelin;`).
- Naming: `PascalCase` (public/types/methods), `camelCase` (params/locals), `_camelCase` (private fields), `IPascalCase` (interfaces).
- Always use braces, even single statements.
- XML docs (`<summary>`/`<param>`/`<returns>`/`<exception>`) on all public APIs.
- `ConfigureAwait(false)` on every await in the library.
- Multi-target `net8.0;net9.0;net10.0` (latest net10.0); `LangVersion=latest`, `Nullable` + `ImplicitUsings` enabled — set in `build.props`.
- Central Package Management: versions in `Directory.Packages.props`; `Microsoft.Extensions.*` are pinned per target framework.

## Testing

Target 80%+ coverage. Tests never touch the network.

- Name tests `MethodName_Scenario_ExpectedBehavior`.
- AAA = Gherkin (Given-When-Then); comment the sections.
- `[Theory]` + `[InlineData]` for multi-scenario logic; every refusal reason has a test.

## Build & Test

```bash
dotnet build Myelin.slnx
dotnet test --solution Myelin.slnx
```

## Commits & Branches

semantic-release derives the next version from commit messages (see `CONTRIBUTING.md`), so these rules decide what gets released.

- Commits and PR titles follow [Conventional Commits](https://www.conventionalcommits.org/): `type(scope)?: summary`, imperative, lowercase, no trailing period.
- `feat` → minor, `fix`/`perf` → patch. `docs`, `test`, `refactor`, `build`, `ci`, `chore`, `style`, `revert` → no release.
- Breaking change: `type!:` or a `BREAKING CHANGE:` footer → major. Required for any public API break.
- PRs are squash-merged with the PR title as the commit message: the PR title is what counts (checked by `pr-title.yml`).
- Branches: `<type>/<issue>-<slug>`, or `<type>/<slug>` without an issue; kebab-case, `type` from the list above.

## Labels

Always label a PR or issue when opening it (`gh pr create --label`, `gh issue create --label`). Use existing labels only (`gh label list`), never create new ones, and add every label that applies.

| Change | Label |
|---|---|
| `feat` / feature request | `enhancement` |
| `fix` / bug report | `bug` |
| `docs` | `documentation` |
| other (`ci`, `chore`, dependencies…) | the closest existing label |
