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
        if ($packageNode.Include -eq $packageRefArm64) {
            $packageNode.Include = $packageRefX64
            Write-Output "Switched arm64 -> x64"
        } elseif ($packageNode.Include -eq $packageRefX64) {
            $packageNode.Include = $packageRefArm64
            Write-Output "Switched x64 -> arm64"
        } else {
            Write-Output "No matching PackageReference found."
        }
    }
}

# Save the updated .csproj file
$coreCsproj.Save($coreProjectPath)
