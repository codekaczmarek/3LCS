# 3LCS

A modern WPF rewrite of [2LCS](https://github.com/microsoft/2LCS) — a client for Microsoft Dynamics 365 **Lifecycle Services (LCS)**.

## Features

- 🖥️ **WPF UI** — non-blocking, async, with loading overlays
- 💉 **Dependency Injection** — `Microsoft.Extensions.Hosting` + `CommunityToolkit.Mvvm`
- 🌍 **Multi-geo support** — Global, EU, UAE, China, or custom LCS URLs
- 🔐 **Auto re-login** — detects session expiry (HTTP 498) and re-authenticates silently
- 🟢 **Environment liveness check** — async TCP probe (port 443/80) with per-row dot indicator
- 📋 **Full environment grid** — all columns matching 2LCS (CHE + SaaS)
- 🔍 **Project chooser** — searchable DataGrid with favourites, sortable columns, double-click select
- 🔄 **F5 refresh** — keyboard shortcut bound to Refresh
- 📡 **API Monitor** — collapsible panel showing live HTTP calls

## Requirements

- Windows 10/11 x64
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) *(framework-dependent build only)*
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

## Download

See the [Releases](../../releases) page for pre-built binaries:

| Artifact | Notes |
|---|---|
| `*-framework-dependent.zip` | Requires .NET 8 Desktop Runtime installed |
| `*-self-contained.zip` | Single EXE, no runtime needed |

## Building from source

```bash
git clone https://github.com/codekaczmarek/3LCS.git
cd 3LCS
dotnet build 3LCS/3LCS.csproj
```

## Release

Push a tag to trigger a GitHub Actions release build:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Or use **Actions → Release → Run workflow** and enter the version manually.
