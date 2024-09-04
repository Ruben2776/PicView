# Define the project path relative to the script's location
$projectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.Avalonia.Win32\PicView.Avalonia.Win32.csproj"

# Load the .csproj file as XML and extract the AssemblyVersion
$projectFile = [xml](Get-Content $projectPath)
$assemblyVersion = $projectFile.Project.PropertyGroup.AssemblyVersion

# Define the temporary output path using the system's temp folder
$tempPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PicView"

# Define the final output path relative to the script's location
$outputPath = Join-Path -Path $PSScriptRoot -ChildPath "PicView-v.$assemblyVersion-win-x64"

# Ensure the temp directory exists
if (-Not (Test-Path $tempPath)) {
    New-Item -Path $tempPath -ItemType Directory | Out-Null
}

# Run dotnet publish
dotnet publish $projectPath --runtime win-x64 --self-contained true --configuration Release --output $tempPath /p:PublishReadyToRun=true

# Ensure the output directory exists and is empty
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}
New-Item -Path $outputPath -ItemType Directory | Out-Null

# Copy the build output to the final destination
Copy-Item -Path "$tempPath\*" -Destination $outputPath -Recurse -Force

# Remove the license file
$licensePath = Join-Path -Path $outputPath -ChildPath "Licenses\XamlAnimatedGif LICENSE.txt"
if (Test-Path $licensePath) {
    Remove-Item -Path $licensePath -Force
}

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
