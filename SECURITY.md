# Security Policy

## Supported versions

Security fixes are released for the latest minor version of the current major version.

| Version | Supported |
|---|---|
| 1.x (latest minor) | ✅ |
| Older versions | ❌ |

Releases are automatic: a fix merged on `main` is published to NuGet within minutes. Update to the latest version
before reporting, in case the issue is already fixed.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions or pull requests.**

Report them privately through GitHub: open the [Security tab](https://github.com/UnambitiousFx/Myelin/security) of
this repository and click **Report a vulnerability**
([direct link](https://github.com/UnambitiousFx/Myelin/security/advisories/new)).

Please include:

- the package and version (UnambitiousFx.Myelin);
- the .NET version and operating system;
- a description of the issue and its impact;
- the smallest code sample or input that reproduces it.

## What to expect

UnambitiousFx is maintained on a best-effort basis by a small team.

- We aim to acknowledge your report within **7 days**.
- We will confirm whether we consider it a vulnerability, and keep you informed while we work on a fix.
- Once a fixed version is on NuGet, we publish a GitHub security advisory and credit you, unless you prefer to stay
  anonymous.

Please give us a reasonable time to release a fix before disclosing the issue publicly.

## Scope

In scope, for example:

- **Any way around the address checks**: a URL, redirect, DNS answer or address notation that makes Myelin connect to a private, loopback, link-local or other special-purpose address it should refuse.
- A redirect followed when it should be refused, or followed without being revalidated.
- A response body read beyond `maxBytes`, or a call running past `Timeout`.
- A refusal log line that contains a request path or query string.

Out of scope: vulnerabilities in the applications that use the library (unless the library causes them), in .NET
itself, or in third-party dependencies (report those upstream; we will update the dependency).
