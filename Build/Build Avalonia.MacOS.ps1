param (
    [Parameter()]
    [string]$Platform,

    [Parameter()]
    [string]$outputPath,

    [Parameter()]
    [string]$appVersion,

    # Optional: e.g. "Developer ID Application: ...". When set, the bundled
    # motion photo dylib is signed (hardened runtime, secure timestamp) before
    # the app bundle itself is signed/notarized.
    [Parameter()]
    [string]$CodesignIdentity
)
# Define the core project path relative to the script's location
$coreProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Core\PicView.Core.csproj"

# Load the .csproj file as XML
[xml]$coreCsproj = Get-Content $coreProjectPath

# Define the package reference to replace
$packageRefX64 = "Magick.NET-Q8-x64"
$packageRefArm64 = "Magick.NET-Q8-arm64"

# Find the Magick.NET package reference and update it based on the platform
$packageNodes = $coreCsproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageRefX64 -or $_.Include -eq $packageRefArm64 }
if ($packageNodes) {
    foreach ($packageNode in $packageNodes) {
        if ($Platform -eq "arm64") {
            $packageNode.Include = $packageRefArm64
        } else {
            $packageNode.Include = $packageRefX64
        }
    }
}

# Save the updated .csproj file
$coreCsproj.Save($coreProjectPath)

# Motion photo native library: build picview-ffmpeg when its dylib is missing,
# so dotnet publish picks it up and it is bundled below. One-time prerequisites:
# brew install zig nasm make
$ffmpegOutputDir = Join-Path -Path $PSScriptRoot -ChildPath "ffmpeg-native/osx-$Platform"
$ffmpegDylibPath = Join-Path -Path $ffmpegOutputDir -ChildPath "libpicviewffmpeg.dylib"
if (-not (Test-Path $ffmpegDylibPath)) {
    Write-Host "libpicviewffmpeg.dylib not found for osx-$Platform - building picview-ffmpeg..."
    foreach ($tool in @('zig', 'nasm', 'brew')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "$tool not found. Install the picview-ffmpeg prerequisites with: brew install zig nasm make"
        }
    }
    # FFmpeg needs GNU make; brew installs it keg-only as gmake, so expose the
    # directory containing it as 'make' to the build script
    $gmakeDir = Join-Path (brew --prefix make) 'libexec/gnubin'
    if (-not (Test-Path (Join-Path $gmakeDir 'make'))) {
        throw "GNU make not found. Install the picview-ffmpeg prerequisites with: brew install zig nasm make"
    }
    $env:PV_EXTRA_PATH = [string]::IsNullOrEmpty($env:PV_EXTRA_PATH) ? $gmakeDir : "$gmakeDir`:$env:PV_EXTRA_PATH"
    & (Join-Path $PSScriptRoot 'Build-FFmpegNative.ps1') -Targets "osx-$Platform"
}

# Define the project path for the actual build target
$avaloniaProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "../src/PicView.Avalonia.MacOS/PicView.Avalonia.MacOS.csproj"

# Create temporary build output directory
$tempBuildPath = Join-Path -Path $outputPath -ChildPath "temp"
New-Item -ItemType Directory -Force -Path $tempBuildPath

# Run dotnet publish for the Avalonia project
dotnet publish $avaloniaProjectPath `
    --runtime "osx-$Platform" `
    --self-contained true `
    --configuration Release `
    -p:UseAppHost=true `
    -p:PublishSingleFile=false `
    --output $tempBuildPath

# Create .app bundle structure
$appBundlePath = Join-Path -Path $outputPath -ChildPath "PicView.app"
$contentsPath = Join-Path -Path $appBundlePath -ChildPath "Contents"
$macOSPath = Join-Path -Path $contentsPath -ChildPath "MacOS"
$resourcesPath = Join-Path -Path $contentsPath -ChildPath "Resources"

# Create directory structure
New-Item -ItemType Directory -Force -Path $macOSPath
New-Item -ItemType Directory -Force -Path $resourcesPath

# Use template Info.plist and patch version and architecture
$infoPlistTemplatePath = Join-Path -Path $PSScriptRoot -ChildPath "../src/PicView.Core.MacOS/Info.plist"
$infoPlistPath = Join-Path -Path $contentsPath -ChildPath "Info.plist"

# Read template as text
$infoPlistContent = Get-Content $infoPlistTemplatePath -Raw

# Map platform identifier to proper macOS architecture identifier
$macOSArchitecture = if ($Platform -eq "arm64") { "arm64" } else { "x86_64" }

# Replace placeholders with actual values
$infoPlistContent = $infoPlistContent -replace "{{appVersion}}", $appVersion
$infoPlistContent = $infoPlistContent -replace "{{platform}}", $macOSArchitecture

# Save Info.plist with UTF-8 encoding without BOM
$utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($infoPlistPath, $infoPlistContent, $utf8NoBomEncoding)

# Copy build output to MacOS directory
Copy-Item -Path "$tempBuildPath/*" -Destination $macOSPath -Recurse

# Copy icon if it exists
$iconSource = Join-Path -Path $PSScriptRoot -ChildPath "../src/PicView.Avalonia.MacOS/Assets/AppIcon.icns"
if (Test-Path $iconSource) {
    Copy-Item -Path $iconSource -Destination $resourcesPath
}

# Remove PDB files
Get-ChildItem -Path $macOSPath -Filter "*.pdb" -Recurse | Remove-Item -Force

# Remove temporary build directory
Remove-Item -Path $tempBuildPath -Recurse -Force

# Set executable permissions on all binaries and dylibs
Get-ChildItem -Path $macOSPath -Recurse | ForEach-Object {
    if ($_.Extension -in @('.dylib', '') -or $_.Name -eq 'PicView.Avalonia.MacOS') {
        chmod +x $_.FullName
    }
}
# Set proper ownership and permissions for the entire .app bundle
chmod -R 755 $appBundlePath

# Sign the bundled motion photo dylib when a signing identity is provided, so
# Gatekeeper/notarization accept it (inside-out: dylib before the app bundle).
# Without an identity the existing bundle signing covers it instead.
$bundledDylibPath = Join-Path -Path $macOSPath -ChildPath "ffmpeg/osx-$Platform/libpicviewffmpeg.dylib"
if (Test-Path $bundledDylibPath) {
    if ($CodesignIdentity) {
        codesign --force --options runtime --timestamp --sign $CodesignIdentity $bundledDylibPath
    }
}
else {
    Write-Warning "libpicviewffmpeg.dylib was not bundled; motion photos will play back as still images."
}