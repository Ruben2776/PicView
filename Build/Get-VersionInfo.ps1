# Get-VersionInfo.ps1

# Load Directory.Build.props as an XML
[xml]$xml = Get-Content "$PSScriptRoot/../src/Directory.Build.props"

# Extract VersionPrefix, VersionSuffix, and FileVersion
$versionPrefix = $xml.Project.PropertyGroup.VersionPrefix
$versionSuffix = $xml.Project.PropertyGroup.VersionSuffix
$fileVersion = $xml.Project.PropertyGroup.FileVersion

# Combine VersionPrefix and VersionSuffix
$fullVersion = "$versionPrefix-$versionSuffix"

# Output the results for GitHub Actions
Write-Output "::set-output name=version::$fullVersion"
Write-Output "::set-output name=file-version::$fileVersion"