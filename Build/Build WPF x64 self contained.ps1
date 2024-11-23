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

$publishPath = Join-Path -Path $PSScriptRoot -ChildPath "PicView-x64-self-contained"

# Run dotnet publish for the project
dotnet publish $projectPath --runtime win-x64 --self-contained true --configuration Release --output $publishPath /p:PublishReadyToRun=true

#rm "$publishPath/System.Windows.Forms*"
rm "$publishPath/Microsoft.VisualBasic.Forms.dll"
rm "$publishPath/Microsoft.Web.WebView2.WinForms.dll"
rm "$publishPath/PicView.pdb"
rm "$publishPath/PicView.Core.pdb"
rm "$publishPath/PicView.dll.config"
rm "$publishPath/createdump.exe" 
rm "$publishPath/XamlAnimatedGif.pdb" 
rm -r "$publishPath/ar"
rm -r "$publishPath/cs"
rm -r "$publishPath/da"
rm -r "$publishPath/de"
rm -r "$publishPath/es"
rm -r "$publishPath/fr"
rm -r "$publishPath/it"
rm -r "$publishPath/ja*"
rm -r "$publishPath/ko"
rm -r "$publishPath/lv"
rm -r "$publishPath/nl"
rm -r "$publishPath/pt*"
rm -r "$publishPath/pl"
rm -r "$publishPath/ru"
rm -r "$publishPath/sk"
rm -r "$publishPath/sv"
rm -r "$publishPath/th"
rm -r "$publishPath/tr"
rm -r "$publishPath/zh*"


#Remove unintended space
if (-not [string]::IsNullOrEmpty($outputPath)) {
    Rename-Item -path $outputPath -NewName $outputPath.Replace(" ","")
}
