# PicView Development Guidelines

## Build & Configuration

- **Solution**: `PicView.slnx` in the `src/` root.
- **Target framework**: `net11.0` (with `net10.0-windows11.0.22621` for Win32 project). Language version set to `preview`.
- **Platforms**: `x64` and `arm64` only — all projects specify `<Platforms>x64;arm64</Platforms>`.
- **AOT**: `PicView.Core` is configured for Native AOT (`PublishAot`, `Trimming=full`, `IsAotCompatible`). Keep new Core code AOT-compatible (no reflection-heavy patterns).
- **Directory.Build.props** (solution root) sets shared properties: `Nullable=enable`, and version info.
- **MacOS project has pre-existing build errors on Windows** — the test project already excludes its reference. When building the full solution on Windows, MacOS build failures are expected and unrelated to your changes.

### Building the test project only (recommended on Windows)

```powershell
cd src
dotnet build PicView.Tests\PicView.Tests.csproj
```

## Testing

### Framework & Setup

- **Framework**: xUnit.v3 with `Microsoft.NET.Test.Sdk`.
- **Avalonia UI tests**: `Avalonia.Headless.XUnit` is available for tests that need the Avalonia rendering pipeline.
- **Test project**: `PicView.Tests/PicView.Tests.csproj` — references `PicView.Core` and `PicView.Avalonia`.
- **Global usings** (`GlobalUsings.cs`): `xunit.v3`, `SettingsManager` (static), `System.Collections.Generic`, `System`.
- **InternalsVisibleTo**: `PicView.Core` exposes internals to `PicView.Tests`.

### Running Tests

- **Agent Guideline**: When running `dotnet test`, never schedule polling/timeout timers (`schedule`). Allow background execution to complete indefinitely and rely on reactive task completion notifications.

Run all tests:

```powershell
cd src
dotnet test PicView.Tests\PicView.Tests.csproj
```

Run a specific test by fully qualified name:

```powershell
dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName=PicView.Tests.FileFunctionTest.TestTemporaryFiles"
```

Run tests in a specific class:

```powershell
dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.SharedImageCacheTests"
```

### Adding New Tests

1. Create a `.cs` file in `PicView.Tests/` (or a subdirectory for organization, e.g., `Navigation/`).
2. Use file-scoped namespace `PicView.Tests` (or `PicView.Tests.SubFolder`).
3. No need to import `Xunit` — it's in `GlobalUsings.cs`.
4. Use `[Fact]` for simple tests, `[Theory]` with `[InlineData]` for parameterized tests.
5. If settings are needed, call `SetDefaults()` in the constructor.

Example test:

```csharp
using PicView.Core.FileHandling;

namespace PicView.Tests;

public class TempFileTests
{
    [Fact]
    public void TempDirectory_CreateAndDelete_ShouldSucceed()
    {
        var created = TempFileHelper.CreateTempDirectory();
        Assert.True(created);
        Assert.True(Directory.Exists(TempFileHelper.TempFilePath));

        var path = TempFileHelper.TempFilePath;
        TempFileHelper.DeleteTempFiles();
        Assert.False(Directory.Exists(path));
    }
}
```

## Code Style

- **File-scoped namespaces** (`namespace X;` not block-scoped).
- **Nullable reference types** enabled globally.
- **Implicit usings** enabled — no need for `using System;` etc.
- **Naming**: PascalCase for public members, `_camelCase` for private fields. Test methods use `MethodOrScenario_Condition_ExpectedResult` pattern.
- **Async patterns**: `async Task` / `ValueTask` used extensively; avoid `async void`.
- **Reactive extensions**: R3 (`ObservableCollections.R3`, `R3`) used for reactive patterns in ViewModels.

## Project Structure

| Project | Purpose                                                                                                                        |
|--|--------------------------------------------------------------------------------------------------------------------------------|
| `PicView.Core`            | Platform-independent core logic (navigation, config, file handling, image models). AOT-compatible.                             |
| `PicView.Avalonia`        | Shared Avalonia UI layer (views, controls, themes).                                                                            |
| `PicView.Avalonia.Win32`  | Windows-specific UI and platform services.                                                                                     |
| `PicView.Avalonia.Linux`  | Linux-specific UI and platform services.                                                                                       |
| `PicView.Avalonia.MacOS`  | macOS-specific UI.                                                                                                             |
| `PicView.Core.WindowsNT`  | Windows-specific core utilities.                                                                                               |
| `PicView.Core.Linux`      | Linux-specific core utilities.                                                                                                 |
| `PicView.Core.MacOS`      | macOS-specific core utilities.                                                                                                 |
| `PicView.Benchmarks`      | BenchmarkDotNet performance benchmarks.                                                                                        |
| `PicView.Tests`           | xUnit.v3 test project.                                                                                                         |
