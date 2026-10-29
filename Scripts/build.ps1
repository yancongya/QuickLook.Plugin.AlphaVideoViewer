# Build and package QuickLook.Plugin.AlphaVideoViewer
param(
    [switch]$Clean,
    [switch]$SkipBuild,
    [switch]$SkipVersion
)

$ErrorActionPreference = "Stop"
$project = "QuickLook.Plugin.AlphaVideoViewer.csproj"
$releaseDir = "bin/Release"
$pluginFile = "QuickLook.Plugin.AlphaVideoViewer.qlplugin"

# Update version
if (-not $SkipVersion) {
    Write-Host "Updating version..." -ForegroundColor Yellow
    powershell -ExecutionPolicy Bypass -File "$PSScriptRoot\update-version.ps1"
}

# Clean
if ($Clean) {
    Write-Host "Cleaning..." -ForegroundColor Yellow
    dotnet clean $project -c Release
    Remove-Item $pluginFile -ErrorAction SilentlyContinue
}

# Build
if (-not $SkipBuild) {
    Write-Host "Building..." -ForegroundColor Yellow
    dotnet build $project -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Package
Write-Host "Packaging..." -ForegroundColor Yellow
Remove-Item $pluginFile -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path $releaseDir -Exclude "*.pdb", "*.xml"
Compress-Archive -Path $files -DestinationPath "$pluginFile.zip"
Move-Item "$pluginFile.zip" $pluginFile -Force

$size = (Get-Item $pluginFile).Length / 1MB
Write-Host "Created: $pluginFile ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
