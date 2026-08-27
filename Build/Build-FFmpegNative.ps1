<#
.SYNOPSIS
Builds picview-ffmpeg, the statically-linked, purpose-built FFmpeg used for motion
photo video playback, for all supported targets:

  win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64

Output lands in Build\ffmpeg-native\<target>\ and is picked up by the platform
projects at build/publish time (motion photos degrade to still images when absent).

.DESCRIPTION
The FFmpeg build is trimmed to exactly what motion photo playback needs:
mov/mp4 demuxer + h264/hevc decoders + libswscale. No audio, no network, no
devices, no filters, no programs. The result is statically linked into a single
native library per target that exports only four functions (pv_open,
pv_decode_next, pv_close, pv_version) - see Native\ffmpeg\picview_ffmpeg.c.

Prerequisites (one-time):
  Windows host (win-x64 builds natively; everything else cross-compiles):
    * MSYS2 (https://www.msys2.org), installed to C:\msys64 by default:
        pacman -Syu
        pacman -S base-devel mingw-w64-x86_64-gcc mingw-w64-x86_64-nasm diffutils tar
    * zig on PATH (https://ziglang.org - only needed for cross-targets, i.e.
      everything except win-x64):
        winget install Zig.Zig    (or scoop install zig)
  macOS host (builds the osx-* targets natively on the host architecture):
    * brew install make            (plus nasm for Intel hosts, zig for cross-targets)
    * GNU make must be reachable as 'make'; set PV_EXTRA_PATH to the directory
      that contains it, e.g. "$((brew --prefix make))/libexec/gnubin"

.PARAMETER Targets
Comma-separated list of targets to build. Defaults to all six.

.PARAMETER Msys2Root
MSYS2 installation root. Defaults to C:\msys64.

.EXAMPLE
.\Build-FFmpegNative.ps1 -Targets win-x64
#>
param (
    [Parameter()]
    [string[]]$Targets = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"),

    [Parameter()]
    [string]$Msys2Root = "C:\msys64"
)

$ErrorActionPreference = "Stop"

$ffmpegVersion = "9.0.1"
$scriptRoot = $PSScriptRoot
$repoRoot = Join-Path $scriptRoot ".."
$shim = Join-Path $repoRoot "Native\ffmpeg\picview_ffmpeg.c"
$outputDir = Join-Path $scriptRoot "ffmpeg-native"

function Test-TargetRequiresZig([string]$target) {
    # win-x64 builds natively with MSYS2's MINGW64 gcc; the osx-* targets build
    # natively on a macOS host of the same architecture. Everything else is
    # cross-compiled through zig cc.
    switch ($target) {
        'win-x64'   { return $false }
        'osx-arm64' { return -not ($IsMacOS -and [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) }
        'osx-x64'   { return -not ($IsMacOS -and [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::X64) }
        default     { return $true }
    }
}

$zigCommand = Get-Command zig -ErrorAction SilentlyContinue
$needsZig = @($Targets | Where-Object { Test-TargetRequiresZig $_ }).Count -gt 0
if ($needsZig -and -not $zigCommand) {
    Write-Error "zig not found on PATH. Install it with: winget install Zig.Zig (or scoop install zig / brew install zig)"
}

if ($IsWindows) {
    $bash = Join-Path $Msys2Root "usr\bin\bash.exe"
    if (-not (Test-Path $bash)) {
        Write-Error "MSYS2 not found at $Msys2Root. Install MSYS2 and run: pacman -S base-devel mingw-w64-x86_64-gcc mingw-w64-x86_64-nasm diffutils tar"
    }
}
else {
    $bash = (Get-Command bash -ErrorAction SilentlyContinue).Source
    if (-not $bash) {
        Write-Error "bash not found on PATH"
    }
}

# Scratch space lives outside the repo; the ffmpeg source is cached there.
$workRoot = Join-Path ([System.IO.Path]::GetTempPath()) "picview-ffmpeg-build"
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
$sourceDir = Join-Path $workRoot "ffmpeg-$ffmpegVersion"

function ConvertTo-ShellPath([string]$path) {
    # MSYS2 bash wants /c/... paths; native shells want the path as-is.
    # GetFullPath (unlike Resolve-Path) also works for paths that do not exist
    # yet, e.g. the output directory on a fresh checkout.
    $full = [System.IO.Path]::GetFullPath($path)
    if (-not $IsWindows) {
        return $full
    }

    $drive = $full.Substring(0, 1).ToLowerInvariant()
    return "/$drive/" + ($full.Substring(3) -replace '\\', '/')
}

# 1. Fetch the FFmpeg source once
if (-not (Test-Path $sourceDir)) {
    $tarball = Join-Path $workRoot "ffmpeg-$ffmpegVersion.tar.xz"
    if (-not (Test-Path $tarball)) {
        Write-Host "Downloading FFmpeg $ffmpegVersion source..."
        Invoke-WebRequest -Uri "https://ffmpeg.org/releases/ffmpeg-$ffmpegVersion.tar.xz" -OutFile $tarball
    }
    Write-Host "Extracting FFmpeg source..."
    if ($IsWindows) {
        & $bash -lc "tar -xf `"`$(cygpath -u '$tarball')`" -C `"`$(cygpath -u '$workRoot')`""
        if ($LASTEXITCODE -ne 0) { throw "Failed to extract FFmpeg source" }
    }
    else {
        tar -xf $tarball -C $workRoot
        if ($LASTEXITCODE -ne 0) { throw "Failed to extract FFmpeg source" }
    }
}

# 2. Build the small Mach-O nm used by configure to detect the '_' symbol prefix
#    when cross-compiling for Apple targets from Windows (GNU nm cannot read
#    Mach-O objects). Apple hosts use their native nm instead.
$appleTargets = @($Targets | Where-Object { $_ -like 'osx-*' })
if ($IsWindows -and $appleTargets.Count -gt 0) {
    $machonm = Join-Path $workRoot "machonm.exe"
    Write-Host "Building machonm helper..."
    zig cc -target x86_64-windows (Join-Path $scriptRoot "ffmpeg\machonm.c") -o $machonm
    if ($LASTEXITCODE -ne 0) { throw "Failed to build machonm" }
}
else {
    $machonm = "nm"
}

# 3. Build each target
# build-target.sh prepends PV_EXTRA_PATH to its PATH; make sure zig (when needed)
# and any caller-provided toolchain directories (e.g. GNU make on macOS) are reachable
if ($zigCommand) {
    $zigDir = ConvertTo-ShellPath (Split-Path $zigCommand.Source)
    $env:PV_EXTRA_PATH = [string]::IsNullOrEmpty($env:PV_EXTRA_PATH) ? $zigDir : "$env:PV_EXTRA_PATH`:$zigDir"
}

$env:PV_ROOT = ConvertTo-ShellPath $workRoot
$env:PV_SRC = ConvertTo-ShellPath $sourceDir
$env:PV_SHIM = ConvertTo-ShellPath $shim
$env:PV_OUT = ConvertTo-ShellPath $outputDir
$env:MACHONM = $IsWindows ? (ConvertTo-ShellPath $machonm) : "nm"
$buildScript = ConvertTo-ShellPath (Join-Path $scriptRoot "ffmpeg\build-target.sh")

foreach ($target in $Targets) {
    Write-Host ""
    Write-Host "########## Building picview-ffmpeg for $target ##########" -ForegroundColor Cyan

    if ($IsWindows) {
        # MSYS2's login shell sets up the MINGW64 toolchain PATH
        $env:MSYSTEM = "MINGW64"
        $env:CHERE_INVOKING = "1"
        & $bash -lc "bash '$buildScript' $target"
    }
    else {
        & $bash "$buildScript" $target
    }
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $target" }
}

Write-Host ""
Write-Host "picview-ffmpeg build complete:" -ForegroundColor Green
Get-ChildItem $outputDir -Recurse -File | ForEach-Object {
    "{0,-45} {1,6:N2} MB" -f $_.FullName.Substring($repoRoot.Length), ($_.Length / 1MB)
}
