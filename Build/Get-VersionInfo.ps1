# Get-VersionInfo.ps1

# Load Directory.Build.props as an XML
[xml]$xml = Get-Content "$PSScriptRoot/../src/Directory.Build.props"

# Extract VersionPrefix, VersionSuffix, and FileVersion
$versionPrefix = $xml.SelectSingleNode("//VersionPrefix")?.InnerText?.Trim()
$versionSuffix = $xml.SelectSingleNode("//VersionSuffix")?.InnerText?.Trim()
$fileVersion = $xml.SelectSingleNode("//FileVersion")?.InnerText?.Trim()

# Combine VersionPrefix and VersionSuffix only if VersionSuffix is not empty
if ([string]::IsNullOrWhiteSpace($versionSuffix)) {
    $fullVersion = $versionPrefix
} else {
    $fullVersion = "$versionPrefix-$versionSuffix"
}

# Output the results for GitHub Actions
"version=$fullVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"file-version=$fileVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"clean-version=$versionPrefix" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8