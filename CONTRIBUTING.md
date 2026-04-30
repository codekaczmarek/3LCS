# Contributing to 3LCS

Thank you for your interest in contributing!

## Before You Start

- Check [open issues](../../issues) to avoid duplicate work.
- For significant changes, open an issue first to discuss the approach.
- All contributions must be compatible with the [MIT License](LICENSE).
  By submitting a pull request you confirm that you have the right to license
  your contribution under the MIT License.

## Development Setup

Requirements:
- Windows 10/11 x64
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

```bash
git clone https://github.com/codekaczmarek/3LCS.git
cd 3LCS
dotnet build 3LCS/3LCS.csproj
```

## Pull Request Guidelines

- Target the `master` branch.
- One logical change per PR — keep PRs small and focused.
- All PRs are merged via **squash merge**; write a clear PR title since it
  becomes the commit message on `master`.
- Ensure the project builds without warnings before submitting:
  ```bash
  dotnet build 3LCS/3LCS.csproj -c Release
  ```
- Describe what the change does and why in the PR description.
- Reference any related issue with `Fixes #<number>` or `Closes #<number>`.

## Code Style

- Follow existing patterns — the project uses
  `CommunityToolkit.Mvvm` source generators and async/await throughout.
- Keep the UI non-blocking: all LCS API calls must be awaited and must not
  run on the UI thread.
- No hardcoded credentials, secrets, or personal URLs.

## Reporting Bugs

Open a [GitHub Issue](../../issues/new) with:
- 3LCS version (visible in the About window)
- Windows version
- Steps to reproduce
- Expected vs. actual behaviour

## Security Issues

See [SECURITY.md](SECURITY.md) — do **not** open public issues for
security vulnerabilities.
