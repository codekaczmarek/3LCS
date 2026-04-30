# 3LCS

A modern WPF rewrite of [2LCS](https://github.com/microsoft/2LCS) — a desktop client for Microsoft Dynamics 365 **Lifecycle Services (LCS)**.

## Features

### Core
- 🖥️ **WPF UI** — fully async, non-blocking UI with loading overlays
- 💉 **Dependency Injection** — `Microsoft.Extensions.Hosting` + `CommunityToolkit.Mvvm`
- 🌍 **Multi-geo support** — Global, EU, UAE, China, or custom LCS URLs
- 🔐 **Auto re-login** — detects session expiry (HTTP 498) and re-authenticates silently
- 📡 **API Monitor** — collapsible panel showing live HTTP calls with timing

### Environments
- 📋 **Full environment grid** — CHE and SaaS environments with all standard columns
- 🟢 **Liveness check** — async TCP probe (port 443/80) with per-row status indicator
- ▶️ **Start / ⏹ Stop** — start and stop cloud-hosted environments from the context menu
- 🖥️ **Open RDP** — launch Remote Desktop directly from the environment context menu
- 📦 **Apply Package** — deploy a package with package selection dialog and mandatory confirmation prompt
- 🔄 **F5 refresh** — keyboard shortcut bound to Refresh

### Projects & Favourites
- 🔍 **Project chooser** — searchable DataGrid with sortable columns and double-click select
- ⭐ **Favourites** — pin environments and projects to the Favourites accordion tab
- 🏷️ **Friendly names** — assign custom display aliases to any favourited environment or project; shown as the primary label with the real name dimmed beneath

### Other
- 🔑 **RDP credential management** — save and reuse RDP credentials per environment
- 📤 **CSV export** — export the environment list to a CSV file
- 🔗 **Custom links** — define and open custom per-session URLs
- ℹ️ **About** — version info and source link

## Requirements

- Windows 10/11 x64
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) *(framework-dependent build only)*
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

## Download

See the [Releases](../../releases) page for pre-built binaries:

| Artifact | Notes |
|---|---|
| `*-framework-dependent.zip` | Requires .NET 10 Desktop Runtime installed |
| `*-self-contained.zip` | Single EXE, no runtime needed |

## License

3LCS is licensed under the [MIT License](LICENSE).

Third-party component notices are in [Third Party Notices.txt](Third%20Party%20Notices.txt).

This project is based on [microsoft/2LCS](https://github.com/microsoft/2LCS) © Microsoft Corporation (MIT License).

## Building from source

```bash
git clone https://github.com/codekaczmarek/3LCS.git
cd 3LCS
dotnet build 3LCS/3LCS.csproj                        # Debug (console visible)
dotnet build 3LCS/3LCS.csproj -c Release             # Release (GUI-only, no console)
```

## Release

Push a tag to trigger a GitHub Actions release build:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Or use **Actions → Release → Run workflow** and enter the version manually.
