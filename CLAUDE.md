# CLAUDE.md — Everywhere Codebase Guide

## Project Overview

Everywhere is a cross-platform AI assistant desktop application written in C# targeting .NET 10. It integrates multiple LLM providers (Claude, OpenAI, Google Gemini, local Ollama), supports the Model Context Protocol (MCP), provides deep OS-level integration (terminal, screen capture, accessibility), and features a plugin/strategy engine for automated task execution. The UI is built with Avalonia (XAML-based, cross-platform).

See `CHANGELOG.md` for the current version and release notes.

---

## Repository Layout

```
/
├── src/                            # All production source code (10 projects)
│   ├── Everywhere.Abstractions/    # Interfaces and core contracts (no dependencies on Core)
│   ├── Everywhere.Core/            # Main application logic, UI, AI, plugins, chat
│   ├── Everywhere.Cloud/           # Everywhere Cloud API integration
│   ├── Everywhere.Windows/         # Windows entry point (Program.cs)
│   ├── Everywhere.Mac/             # macOS entry point (Program.cs)
│   ├── Everywhere.Linux/           # Linux entry point (Program.cs, early dev)
│   ├── Everywhere.Watchdog/        # Background watchdog service
│   ├── Everywhere.I18N/            # Localization resources
│   ├── Everywhere.I18N.SourceGenerator/         # Roslyn source generator for i18n
│   └── Everywhere.Configuration.SourceGenerator/ # Roslyn source generator for settings
├── tests/
│   ├── Everywhere.Abstractions.Tests/  # NUnit tests for abstractions
│   ├── Everywhere.Core.Tests/          # NUnit tests for core
│   └── Everywhere.Terminal.TestApp/    # Manual test app for terminal features
├── 3rd/                            # Vendored/patched third-party libraries
│   ├── EverythingNetCore/          # Windows Everything file search integration
│   ├── MessagePack-CSharp/         # MessagePack with source generator
│   ├── Porta.Pty/                  # Cross-platform PTY implementation
│   ├── WritableJsonConfiguration/  # JSON config with file watching
│   ├── semantic-kernel/            # Semantic Kernel fork
│   ├── semantic-kernel-patch/      # Custom Google Gemini connector for SK
│   └── shad-ui/                    # Custom Avalonia UI component library
├── patches/                        # Patch files applied during build
├── docs/                           # Developer documentation
│   ├── build.md                    # Build prerequisites and instructions
│   ├── patches.md                  # Documentation on applied patches
│   ├── StrategyEngine/             # Strategy engine architecture docs
│   └── Terminal/                   # Terminal integration docs
├── tools/                          # Build scripts and utilities
├── img/                            # Application icons and images
├── .github/workflows/              # CI/CD pipelines
├── Everywhere.slnx                 # Main solution (all platforms)
├── Everywhere.Windows.slnx         # Windows-only solution filter
├── Everywhere.Mac.slnx             # macOS-only solution filter
├── Everywhere.Linux.slnx           # Linux-only solution filter
├── Directory.Build.props           # Global MSBuild properties (applied to all projects)
├── Directory.Packages.props        # Centralized NuGet version management (CPM)
├── global.json                     # .NET SDK version pin (10.0.202)
└── nuget.config                    # NuGet package sources
```

### Key directories inside `src/Everywhere.Core/`

```
Everywhere.Core/
├── AI/                  # LLM kernel mixins (Claude, OpenAI, Ollama, Google)
├── Chat/                # Chat context, history, service, plugins, permissions
│   ├── Plugins/         # Chat plugin system (built-in tools + MCP)
│   ├── Permissions/     # Tool-use permission/approval system
│   └── VisualContext/   # Screen capture and visual element analysis
├── Common/              # App startup (Entrance.cs), ServiceLocator, shared helpers
├── Configuration/       # Settings, API keys, persistent configuration
├── Database/            # EF Core SQLite context, migrations, interceptors
├── Initialization/      # Per-subsystem initializers (chat, network, settings, updater)
├── Storage/             # Chat persistence helpers
├── StrategyEngine/      # Automated workflow execution engine
├── Terminal/            # PTY-based terminal integration
├── ViewModels/          # MVVM ViewModels (CommunityToolkit.Mvvm)
├── Views/               # Avalonia XAML views (.axaml + .axaml.cs)
├── Web/                 # Web search and page content extraction
├── Interop/             # Platform interop helpers
└── Extensions/          # Extension methods and utilities
```

---

## Technology Stack

| Category | Technology |
|---|---|
| Language | C# 12+ (LangVersion: preview) |
| Runtime | .NET 10.0 |
| UI Framework | Avalonia 11.x (cross-platform XAML) |
| Text Editor | AvaloniaEdit |
| Markdown Rendering | LiveMarkdown.Avalonia |
| UI Components | ShadUI (vendored in `3rd/shad-ui`) |
| LLM Orchestration | Microsoft.SemanticKernel 1.75.x |
| LLM Abstractions | Microsoft.Extensions.AI 10.5.x |
| Claude Integration | Anthropic SDK 12.x |
| OpenAI Integration | Microsoft.Extensions.AI.OpenAI |
| Local LLM | OllamaSharp |
| MCP Protocol | ModelContextProtocol 1.2.x |
| Data Persistence | Entity Framework Core 10 + SQLite |
| Configuration | WritableJsonConfiguration (vendored) |
| Terminal | Porta.Pty (vendored, cross-platform PTY) |
| Web Automation | PuppeteerSharp |
| Secrets | GnomeStack.Os.Secrets (OS credential manager) |
| Logging | Serilog |
| Error Tracking | Sentry |
| MVVM | CommunityToolkit.Mvvm |
| Reactive Collections | DynamicData |
| Testing | NUnit 4 + NSubstitute + coverlet |

---

## Build Prerequisites

- **Git** with LFS and submodule support
- **.NET SDK 10.0.202** (pinned in `global.json`)
- **Windows 10 19041+** / **macOS 12+** / **Linux with X11**
- **JetBrains Rider 2026.1 Nightly+** (recommended IDE; required for `.axaml` IntelliSense)
- **Xcode Command Line Tools** (macOS only)
- **Inno Setup** (Windows installer builds only, handled in CI)

---

## Development Workflows

### Initial Setup

```bash
# Clone with submodules and LFS
git clone https://github.com/Sylinko/Everywhere.git --recursive

# If already cloned without submodules
git submodule update --init --recursive
git lfs pull

# Windows only: enable long paths (run as admin)
git config --global core.longpaths true

# macOS only: install Xcode CLI tools
xcode-select --install
```

### Restore and Build

```bash
# Restore .NET workloads (Avalonia needs specific workloads)
dotnet workload restore Everywhere.slnx

# Restore NuGet packages (includes vendored local packages)
dotnet restore Everywhere.slnx

# Build a specific platform
dotnet build src/Everywhere.Windows/Everywhere.Windows.csproj -c Debug
dotnet build src/Everywhere.Mac/Everywhere.Mac.csproj -c Debug
dotnet build src/Everywhere.Linux/Everywhere.Linux.csproj -c Debug
```

### Run

```bash
dotnet run --project src/Everywhere.Windows/Everywhere.Windows.csproj -c Debug
```

Windows output: `src/Everywhere.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Everywhere.exe`

Using Rider is strongly preferred — it sets the correct working directory and environment automatically.

### Kill Stale Build Servers

If build fails due to locked files:

```bash
dotnet build-server shutdown
```

### Run Tests

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Tests live under `tests/`. The `Everywhere.Terminal.TestApp` is a manual test harness, not automated.

---

## Solution Files

| File | Purpose |
|---|---|
| `Everywhere.slnx` | Full solution — all platforms |
| `Everywhere.Windows.slnx` | Windows builds only |
| `Everywhere.Mac.slnx` | macOS builds only |
| `Everywhere.Linux.slnx` | Linux builds only |

Use platform-specific `.slnx` files for CI. Use `Everywhere.slnx` in your IDE.

---

## Code Conventions

### Nullability

Nullable reference types are **enabled** everywhere (`<Nullable>enable</Nullable>`). Nullable warnings are treated as **errors** (`<WarningsAsErrors>nullable</WarningsAsErrors>`). Never suppress nullability warnings without a clear justification.

### Language Version

C# preview features are enabled (`<LangVersion>preview</LangVersion>`). Use modern C# idioms freely.

### Unsafe Code

`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` is set globally. Unsafe code is used in PTY and platform interop. Keep it isolated and well-commented.

### Implicit Usings

`<ImplicitUsings>enable</ImplicitUsings>` is set. `NUnit.Framework` is globally included in test projects.

### Conditional Compilation

Platform-specific code uses preprocessor constants defined in `Directory.Build.props`:

| Constant | When active |
|---|---|
| `WINDOWS` / `IsWindows` | Building on Windows |
| `LINUX` / `IsLinux` | Building on Linux |
| `OSX` / `MACOS` / `IsOSX` / `IsMacOS` | Building on macOS |
| `UNIX` / `IsUnix` | Linux or macOS |
| `DESKTOP` / `IsDesktop` | Any desktop platform |
| `X64` / `IsX64` | x86-64 architecture |
| `ARM64` / `IsARM64` | ARM64 architecture |
| `X86` / `IsX86` | x86 architecture |
| `ARM` / `IsARM` | ARM architecture |

Use `#if WINDOWS` style guards for platform-specific code paths.

### MVVM Pattern

- ViewModels use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`, etc.).
- Views are Avalonia `.axaml` files with code-behind `.axaml.cs`.
- Bindings use compiled bindings by default (`<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`).
- Reactive collections use `DynamicData`.

### Dependency Injection

- `Microsoft.Extensions.DependencyInjection` is the DI container.
- Services are registered in platform entry points and `Entrance.cs` (`src/Everywhere.Core/Common/`).
- Use constructor injection; avoid `ServiceLocator` except at composition roots.

### Naming

- Follow standard C# conventions (PascalCase types, camelCase locals, `_camelCase` private fields).
- Interfaces are prefixed with `I` (e.g., `IChatService`).
- Source-generated configuration classes use `SettingsAttributes` conventions.

### Avalonia XAML

- UI files use the `.axaml` extension (not `.xaml`).
- Older versions of Rider may show errors in `.axaml` files — use Rider 2026.1 Nightly+.
- Resources and assets are declared in `Everywhere.Core.csproj` as `<AvaloniaResource>`.

---

## Package Management

Package versions are centralized in `Directory.Packages.props` (Central Package Management). **Do not specify versions in individual `.csproj` files** — add/update versions only in `Directory.Packages.props`.

---

## Vendored Dependencies (`3rd/`)

These are Git submodules with custom patches applied at build time via `src/Build.Patches.targets`.

- `Porta.Pty` — cross-platform PTY; local NuGet output goes to `3rd/Porta.Pty/nuget/`
- `shad-ui` — Avalonia UI component library (ShadUI)
- `semantic-kernel-patch` — adds Google Gemini connector to Semantic Kernel
- `WritableJsonConfiguration` — JSON config with live reload
- `MessagePack-CSharp` — used as a source generator analyzer only
- `EverythingNetCore` — Windows-only Everything file search integration

When modifying vendored code, understand which patch file (in `patches/`) governs it and update accordingly. See `docs/patches.md`.

---

## Internationalization (I18N)

- Localization strings live in `src/Everywhere.I18N/Strings.*.resx` files (a dedicated project, not inside Core).
- These are consumed by the `Everywhere.I18N.SourceGenerator` Roslyn analyzer to generate type-safe accessors.
- The `sync-i18n.yml` CI workflow syncs translations.
- Add new strings to the base `.resx` file; the source generator handles the rest.

---

## Configuration System

- Settings are persisted as JSON via `WritableJsonConfiguration`.
- Settings classes use `SettingsAttributes` (from `Everywhere.Configuration.SourceGenerator`) for source-generated boilerplate.
- API keys are stored in the OS credential manager via `GnomeStack.Os.Secrets`.
- Settings migrations are supported — add a migration class when changing the schema.
- Configuration categories live in `src/Everywhere.Core/Configuration/`.

---

## Database

- SQLite database managed by Entity Framework Core 10.
- `DbContext` is in `src/Everywhere.Core/Database/`.
- Add migrations with `dotnet ef migrations add <Name> --project src/Everywhere.Core`.
- The database stores chat history and related metadata.

---

## AI Integration

- `src/Everywhere.Core/AI/` contains per-provider "kernel mixin" classes that extend `IKernelBuilder`.
- The abstraction layer is `Microsoft.Extensions.AI` (`IChatClient`).
- Semantic Kernel (`Kernel`) orchestrates multi-step tool use.
- MCP servers are integrated via the `ModelContextProtocol` package.
- Token counting uses `Microsoft.ML.Tokenizers` (O200k base for GPT-family).
- For Claude specifically, the `Anthropic` SDK is used directly in some paths.

---

## Plugin System

- Chat plugins live in `src/Everywhere.Core/Chat/Plugins/`.
- Plugins are SK functions exposed to the LLM via tool calling.
- A `ChatPluginManager` manages registration and lifecycle.
- User-facing permission prompts are handled via `src/Everywhere.Core/Chat/Permissions/`.
- MCP servers can be connected as external plugin sources.

---

## Strategy Engine

- Location: `src/Everywhere.Core/StrategyEngine/`
- Automates multi-step workflows without continuous user interaction.
- Architecture documented in `docs/StrategyEngine/`.

---

## Terminal Integration

- PTY-based via `Porta.Pty` (cross-platform).
- Shell integration scripts: `src/Everywhere.Core/Assets/Terminal/shellIntegration.{ps1,zsh,bash}`.
- Architecture documented in `docs/Terminal/`.
- Windows ConPTY requires `conpty.dll` copied to the output directory (handled by a build target in the test project).

---

## CI/CD Pipelines

| Workflow | Trigger | Purpose |
|---|---|---|
| `windows-release.yml` | Tag `v*.*.*` or manual | Build Windows MSI installer |
| `macos-release.yml` | Tag `v*.*.*` or manual | Build macOS DMG/app bundle |
| `linux-release.yml` | Tag `v*.*.*` or manual | Build Linux AppImage |
| `aur-publish.yml` | Release | Publish to AUR (Arch Linux) |
| `cla.yml` | PR events | Sylinko CLA Bot |
| `sync-i18n.yml` | Push | Sync translation files |
| `close-stale-issues.yml` | Schedule | Automated issue hygiene |

Releases are triggered by pushing a `v*.*.*` tag. Workflows can also be triggered manually (`workflow_dispatch`) — they validate that the current commit is tagged before proceeding.

---

## Key Files Quick Reference

| File | Purpose |
|---|---|
| `global.json` | SDK version pin |
| `Directory.Build.props` | Global MSBuild properties, platform constants |
| `Directory.Packages.props` | Centralized NuGet version management |
| `src/Everywhere.Core/Common/Entrance.cs` | App startup and DI registration |
| `src/Everywhere.Core/App.axaml.cs` | Avalonia Application class |
| `src/Everywhere.Core/Global.cs` | Global constants and helpers |
| `src/Everywhere.Windows/Program.cs` | Windows entry point |
| `src/Everywhere.Mac/Program.cs` | macOS entry point |
| `src/Everywhere.Linux/Program.cs` | Linux entry point |
| `src/Everywhere.Core/Chat/ChatService.cs` | Core chat orchestration |
| `src/Everywhere.Core/Configuration/` | App settings and API key storage |
| `src/Everywhere.Core/Database/` | EF Core DbContext and migrations |
| `docs/build.md` | Full build instructions |
| `CHANGELOG.md` | Version history and release notes |
