param (
    [Parameter()]
    [string]$Platform,
    
    [Parameter()]
    [string]$outputPath
)

# Define the core project path relative to the script's location
$coreProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Core\PicView.Core.csproj"

# Load the .csproj file as XML
[xml]$coreCsproj = Get-Content $coreProjectPath

# Define the package reference to replace
$packageRefX64 = "Magick.NET-Q8-OpenMP-x64"
$packageRefArm64 = "Magick.NET-Q8-OpenMP-arm64"

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

# Define the project path for the actual build target
$avaloniaProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Avalonia.Win32\PicView.Avalonia.Win32.csproj"

# Run dotnet publish for the Avalonia project
dotnet publish $avaloniaProjectPath --runtime "win-$Platform" --self-contained true --configuration Release --output $outputPath /p:PublishReadyToRun=true


# Remove the PDB file
$pdbPath = Join-Path -Path $outputPath -ChildPath "PicView.Avalonia.pdb"
if (Test-Path $pdbPath) {
    Remove-Item -Path $pdbPath -Force
}

#Remove uninstended space
Rename-Item -path $outputPath -NewName $outputPath.Replace(" ","")


