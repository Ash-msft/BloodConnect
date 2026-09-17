# package.ps1
# Packages the Teams app manifest and icons into a sideload-ready .zip archive
# (teams-app/BloodConnect.teams.zip), per Microsoft Teams app packaging requirements:
# the manifest.json, color.png, and outline.png must sit at the root of the zip
# (no subfolders).
#
# Usage:
#   powershell -File .\package.ps1
#
# Before packaging for real use, edit manifest.json to replace:
#   - "id" and "webApplicationInfo.id" with a real Azure AD App Registration client id
#   - "REPLACE_WITH_YOUR_APP_HOST" with your deployed frontend host name
#   - developer/privacy/terms URLs with your organization's real URLs

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$manifestPath = Join-Path $root 'manifest.json'
$colorPath = Join-Path $root 'color.png'
$outlinePath = Join-Path $root 'outline.png'
$zipPath = Join-Path $root 'BloodConnect.teams.zip'

foreach ($required in @($manifestPath, $colorPath, $outlinePath)) {
    if (-not (Test-Path $required)) {
        throw "Required Teams app asset not found: $required"
    }
}

# Validate manifest.json is well-formed JSON before packaging.
Get-Content $manifestPath -Raw | ConvertFrom-Json | Out-Null

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path $manifestPath, $colorPath, $outlinePath -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Created Teams app package: $zipPath"
Write-Host "Sideload it via Microsoft Teams > Apps > Manage your apps > Upload an app > Upload a custom app."
