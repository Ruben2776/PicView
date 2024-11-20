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

            # Save the updated .csproj file
            $coreCsproj.Save($coreProjectPath)

            Write-Output "Switched arm64 -> x64"
        }
    }
}

# Define the project path for the actual build target
$projectPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\PicView.WPF\PicView.WPF.csproj"

# Run dotnet publish for the project
dotnet publish $projectPath --runtime win-x64 --self-contained true --configuration Release --output "$PSScriptRoot/PicView-x64-self-contained" /p:PublishReadyToRun=true

#Remove unintended space
if (-not [string]::IsNullOrEmpty($outputPath)) {
    Rename-Item -path $outputPath -NewName $outputPath.Replace(" ","")
}
