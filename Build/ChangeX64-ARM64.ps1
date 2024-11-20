# Define the core project path relative to the script's location
$coreProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Core\PicView.Core.csproj"

# Load the .csproj file as XML
[xml]$coreCsproj = Get-Content $coreProjectPath

# Define the package references for x64 and arm64
$packageRefX64 = "Magick.NET-Q8-OpenMP-x64"
$packageRefArm64 = "Magick.NET-Q8-OpenMP-arm64"

# Find the current package reference
$packageNodes = $coreCsproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageRefX64 -or $_.Include -eq $packageRefArm64 }

if ($packageNodes) {
    foreach ($packageNode in $packageNodes) {
        if ($packageNode.Include -eq $packageRefArm64) {
            # Switch from arm64 to x64
            $packageNode.Include = $packageRefX64
            Write-Output "Switched arm64 -> x64"
        } elseif ($packageNode.Include -eq $packageRefX64) {
            # Switch from x64 to arm64
            $packageNode.Include = $packageRefArm64
            Write-Output "Switched x64 -> arm64"
        }
    }
} else {
    Write-Output "No Magick.NET package references found. Adding a new one for arm64."
    # Add a new PackageReference if none exists
    $newNode = $coreCsproj.CreateElement("PackageReference")
    $newNode.SetAttribute("Include", $packageRefArm64)
    $coreCsproj.Project.ItemGroup[0].AppendChild($newNode) | Out-Null
}

# Save the updated .csproj file
$coreCsproj.Save($coreProjectPath)
