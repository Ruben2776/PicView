# Define the core project path relative to the script's location
$coreProjectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Core\PicView.Core.csproj"

# Load the .csproj file as XML
[xml]$coreCsproj = Get-Content $coreProjectPath

# Define the package reference to replace
$packageRefX64 = "Magick.NET-Q8-OpenMP-x64"
$packageRefArm64 = "Magick.NET-Q8-OpenMP-arm64"

# Define the platform target (change this to 'x64' if building for x64)
$platform = "arm64"

# Find the Magick.NET package reference and update it based on the platform
$packageNodes = $coreCsproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageRefX64 -or $_.Include -eq $packageRefArm64 }
if ($packageNodes) {
    foreach ($packageNode in $packageNodes) {
        if ($platform -eq "arm64") {
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

# Load the .csproj file as XML to extract the AssemblyVersion
$avaloniaProjectFile = [xml](Get-Content $avaloniaProjectPath)
$assemblyVersion = $avaloniaProjectFile.Project.PropertyGroup.AssemblyVersion

# Define the temporary output path using the system's temp folder
$tempPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PicView"

# Define the final output path relative to the script's location
$outputPath = Join-Path -Path $PSScriptRoot -ChildPath "PicView-v.$assemblyVersion-win-$platform"

# Ensure the temp directory exists
if (-Not (Test-Path $tempPath)) {
    New-Item -Path $tempPath -ItemType Directory | Out-Null
}

# Run dotnet publish for the Avalonia project
dotnet publish $avaloniaProjectPath --runtime "win-$platform" --self-contained true --configuration Release --output $tempPath /p:PublishReadyToRun=true

# Ensure the output directory exists and is empty
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}
New-Item -Path $outputPath -ItemType Directory | Out-Null

# Copy the build output to the final destination
Copy-Item -Path "$tempPath\*" -Destination $outputPath -Recurse -Force

# Remove the PDB file
$pdbPath = Join-Path -Path $outputPath -ChildPath "PicView.Avalonia.pdb"
if (Test-Path $pdbPath) {
    Remove-Item -Path $pdbPath -Force
}

#Remove uninstended space
Rename-Item -path $outputPath -NewName $outputPath.Replace(" ","")

# Clean up the temporary directory
Start-Sleep -Seconds 2
Remove-Item -Path $tempPath -Recurse -Force
