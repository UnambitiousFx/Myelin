# Contributing to UnambitiousFx.Myelin

Thank you for considering contributing to UnambitiousFx.Myelin!

## Code of Conduct

We expect all contributors to be respectful and considerate, to accept constructive criticism gracefully, and to focus
on what is best for the community.

## How Can I Contribute?

### Reporting Bugs

Check existing issues first, then use the **bug report template**.

**A way around the address checks is a security issue: do not open a public issue for it.** Report it privately through
the repository's *Security* tab (« Report a vulnerability »).

### Suggesting Enhancements

Use the **feature request template**: the use case, the proposed API, and the alternatives you considered.

### Pull Requests

1. Create your branch from `main` (`<type>/<issue>-<slug>`, e.g. `feat/12-ipv6-zone-ids`).
2. Follow the coding standards below.
3. Add tests; a change to the address policy cites the IANA registry entry it follows.
4. Update the XML docs, the README and `docs/` if the public API changes.
5. Run `dotnet test --solution Myelin.slnx`.
6. Run the benchmarks if the change touches the hot path.

PR titles follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `perf:`, `docs:`,
`test:`, `refactor:`, `build:`, `ci:`, `chore:`): PRs are squash-merged and the title becomes the commit that decides the
release (see [Release Process](#release-process)).

## Development Environment Setup

### Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download), plus the .NET 8, 9 and 10 runtimes to run the tests on every target
- An IDE: [JetBrains Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/), or
  [VS Code](https://code.visualstudio.com/)

### Getting Started

```bash
git clone https://github.com/UnambitiousFx/Myelin.git
cd Myelin
dotnet build Myelin.slnx
dotnet test --solution Myelin.slnx
```

### Project Structure

```
Myelin/
├── src/Myelin/                  # The library
├── test/Myelin.Tests/           # Tests
├── benchmarks/MyelinBenchmark/  # BenchmarkDotNet benchmarks
├── examples/BasicUsage/         # Console sample, published with NativeAOT in CI
└── docs/                        # Docusaurus site
```

## Coding Standards

- File-scoped namespaces (`namespace UnambitiousFx.Myelin;`), braces on every block, modern C# syntax.
- `PascalCase` for public members and types, `camelCase` for parameters and locals, `_camelCase` for private fields.
- XML documentation on every public API, with `<exception>` for every exception it throws.
- No runtime dependency beyond `Microsoft.Extensions.Http`, `Logging.Abstractions` and `Options`; nothing that breaks
  trimming or NativeAOT.
- Never log a request path or query: they may carry secrets. Host and reason only.

## Testing Your Changes

```bash
# All tests, on net8.0, net9.0, net10.0 and net11.0
dotnet test --solution Myelin.slnx

# With coverage (Microsoft Testing Platform)
dotnet test --solution Myelin.slnx --coverage --coverage-output-format cobertura --coverage-settings ./test/coverage.settings.xml

# The NativeAOT sample
dotnet publish examples/BasicUsage -c Release -r <rid> --self-contained
```

Tests never reach the network: use `StubHandler` for responses and `FixedResolver` for DNS answers.

### Running Benchmarks

```bash
dotnet run --project benchmarks/MyelinBenchmark -c Release
```

## Release Process

(For maintainers)

Releases are automatic. Every push to `main` that passes CI runs [semantic-release](https://semantic-release.gitbook.io/)
(`.github/workflows/release.yml`):

- `feat:` → minor, `fix:`/`perf:` → patch, `!` or a `BREAKING CHANGE:` footer → major; other types don't release.
- The packages are pushed to NuGet first, then the `vX.Y.Z` tag and the GitHub release are created.

While the repository is private, the release job and the documentation deployment are skipped.

## License

By contributing to UnambitiousFx.Myelin, you agree that your contributions will be licensed under the [MIT License](LICENSE).
