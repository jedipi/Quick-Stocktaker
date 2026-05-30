# AGENTS.md - QuickStocktaker

This file governs the entire repository. Follow these instructions when acting as an agent in this project.

## Project Overview

Quick Stocktaker is a free stock and inventory counting mobile app. It lets users scan product barcodes, store stocktake data locally, and export or send results through CSV, email, and upload-oriented services.

Main stack:
- .NET MAUI mobile app in `src/QuickStockTaker`, currently targeting `net10.0-android` and `net10.0-ios`.
- Shared app logic in `src/QuickStockTaker.Core`, targeting `net10.0`, `net10.0-android`, and `net10.0-ios`.
- xUnit test project in `src/QuickStockTaker.UnitTest`, targeting `net10.0`.
- C#, XAML, CommunityToolkit.Mvvm, Autofac, sqlite-net-pcl, CsvHelper, FluentValidation, MailKit, Serilog, ZXing.Net.Maui, and MAUI Community Toolkit.

Important build-state note:
- The project files are the source of truth and currently target .NET 10.
- `.github/workflows/main.yml` and `azure-pipelines.yml` are aligned to .NET 10 and build/publish `net10.0-android`.
- `README.md` may still contain older .NET 8 references. If you touch build targets, CI, or release docs, align these files intentionally rather than copying stale values.
- There is no `global.json`, `.editorconfig`, `Directory.Build.props`, or `Directory.Packages.props` at the root. Package versions are declared directly in project files.

## Setup & Commands

Prerequisites:
- Install a .NET SDK that can build .NET 10 projects.
- Install MAUI workloads:

```powershell
dotnet workload install maui android ios
```

- Android command-line builds also require an Android SDK. If the SDK is installed in a custom location, pass it explicitly:

```powershell
dotnet build src\QuickStockTaker\QuickStockTaker.csproj -f net10.0-android -c Debug -p:AndroidSdkDirectory="C:\Path\To\Android\Sdk"
```

Restore:

```powershell
dotnet restore src\QuickStockTaker.sln
```

Build the testable Core project:

```powershell
dotnet build src\QuickStockTaker.Core\QuickStockTaker.Core.csproj -f net10.0 -c Debug
```

Run unit tests:

```powershell
dotnet test src\QuickStockTaker.UnitTest\QuickStockTaker.UnitTest.csproj -f net10.0 -v:minimal
```

Build the Android MAUI app:

```powershell
dotnet build src\QuickStockTaker\QuickStockTaker.csproj -f net10.0-android -c Debug
```

Run the Android MAUI app on an attached device or emulator:

```powershell
dotnet build src\QuickStockTaker\QuickStockTaker.csproj -t:Run -f net10.0-android -c Debug
```

Publish the Android MAUI app:

```powershell
dotnet publish src\QuickStockTaker\QuickStockTaker.csproj -f net10.0-android -c Release
```

Build iOS on a supported macOS/Xcode environment:

```powershell
dotnet build src\QuickStockTaker\QuickStockTaker.csproj -f net10.0-ios -c Debug
```

Format/lint check:

```powershell
dotnet format src\QuickStockTaker.sln --verify-no-changes --no-restore -v:minimal
```

## Coding Standards

General C# style:
- Match the style of the file you are editing. The app project commonly uses file-scoped namespaces; many Core files use block-scoped namespaces.
- Prefer 4-space indentation for new code. Do not reformat unrelated existing code.
- Use PascalCase for types, public members, pages, view models, services, validators, and models.
- Use `_camelCase` for private fields and mark them `readonly` when the field is assigned only in the constructor.
- Use `async`/`await` for new asynchronous flows. Avoid adding new `.Wait()` or `.Result` calls.
- Keep nullable changes scoped. The test project enables nullable; the app and Core projects currently do not.

Architecture patterns:
- Keep UI pages and XAML in `src/QuickStockTaker/Views`.
- Keep ViewModels in `src/QuickStockTaker.Core/ViewModels`.
- Use CommunityToolkit.Mvvm attributes such as `[ObservableProperty]` and `[RelayCommand]` for ViewModel state and commands.
- Pages should receive their ViewModel through constructor injection and set `BindingContext` there.
- Register Shell routes in `AppShell.xaml.cs` using `nameof(PageType)` and navigate with `Shell.Current.GoToAsync`.
- Register services through Autofac in `ServiceLocator.RegisterType`. Existing conventions auto-register classes ending in `Page`, `ViewModel`, `Service`, and `Validator`.
- New services should implement interfaces under `src/QuickStockTaker.Core/Services/Interfaces` when they are consumed across boundaries or registered as implemented interfaces.
- Persistence should go through `ISQLiteRepository<T>` / `SQLiteRepository<T>` where practical. SQLite models live under `Models/Sqlite` and derive from `BaseModel`.
- Shared preference keys belong in `QuickStockTaker.Core/Data/Constants.cs`; do not scatter new string literals for preference keys.
- Validation belongs in FluentValidation validators under `QuickStockTaker.Core/Validators`.
- Logging should use existing Serilog and `Microsoft.Extensions.Logging` plumbing. Do not introduce another logging framework.

Testing style:
- Add tests to `src/QuickStockTaker.UnitTest`.
- Use xUnit. FluentAssertions, Bogus, AutoBogus.NSubstitute, NSubstitute, and AutofacContrib.NSubstitute are already referenced for richer tests.
- Prefer focused unit tests around Core services, validators, repositories where feasible, and ViewModel behavior with mocked dependencies.

## Task Execution

Before coding:
- Inspect the relevant files first. State important assumptions when the task has meaningful ambiguity.
- Keep changes surgical. Every changed line should trace to the request.
- Check `git status --short` before editing and preserve unrelated local changes.
- For cleanup/refactor/deslop work, write a short cleanup plan before edits and lock existing behavior with tests where feasible.

During implementation:
- Prefer deletion and reuse over new abstractions.
- Do not add new dependencies unless explicitly requested or clearly necessary for the task; if needed, use `dotnet add package` rather than hand-editing package XML.
- Keep ViewModel, service, validation, persistence, and page responsibilities separated.
- If touching build target frameworks, update project files, README commands, GitHub Actions, and Azure Pipelines together or explicitly report what remains stale.

Verification workflow:
- For Core, service, validator, repository, and ViewModel changes, run:

```powershell
dotnet test src\QuickStockTaker.UnitTest\QuickStockTaker.UnitTest.csproj -f net10.0 -v:minimal
```

- For app startup, XAML, resource, platform, or MAUI project changes, also run a targeted platform build when the local SDKs are available:

```powershell
dotnet build src\QuickStockTaker\QuickStockTaker.csproj -f net10.0-android -c Debug
```

- For formatting-sensitive work, run:

```powershell
dotnet format src\QuickStockTaker.sln --verify-no-changes --no-restore -v:minimal
```

- If a command cannot run because a local prerequisite is missing, report the exact blocker and the command that was attempted.

## Dos and Don'ts

Do:
- Treat `src/QuickStockTaker.sln` and the project files as the canonical build configuration.
- Keep README and CI build commands aligned with project target frameworks when editing build configuration.
- Use `Constants.cs` for shared preference keys.
- Use `nameof(...)` for route registration and type-linked navigation names.
- Keep platform-specific code under the appropriate `Platforms` folder.
- Keep MAUI assets under `Resources` and update project item groups only when necessary.
- Surface existing warnings separately from new warnings when reporting verification.

Don't:
- Do not commit generated `bin`, `obj`, `AppPackages`, `publish`, `.vs`, logs, package caches, local SQLite databases, or `.omx` state.
- Do not commit credentials, SMTP secrets, publish profiles, signing keys, `.pfx` files, keystores, or local machine paths.
- Do not change `ApplicationId`, app version fields, signing/release metadata, or target frameworks unless the task explicitly calls for it.
- Do not bulk-format the repository as part of unrelated work.
- Do not rename public or existing misspelled types such as `FtpUplodService` unless the task includes the migration.
- Do not replace Autofac/MAUI/CommunityToolkit patterns with a different architecture without an explicit architectural task.
- Do not hard-code export directories; use MAUI `FileSystem.AppDataDirectory` or the existing storage abstractions.
