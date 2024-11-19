param (
    [string]$platform = "x64"  # Default to x64 if no parameter is passed
)
param (
    [string]$outputPath = $PSScriptRoot
)
param (
    [bool]$selfContained = $True
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
$projectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.WPF\PicView.WPF.csproj"

# Run dotnet publish for the project
dotnet publish $projectPath --runtime "win-$platform" --self-contained $selfContained --configuration Release --output $outputPath /p:PublishReadyToRun=true


#Remove uninstended space
Rename-Item -path $outputPath -NewName $outputPath.Replace(" ","")


