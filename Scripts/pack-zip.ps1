# Quick packaging script (run from project root)
$ErrorActionPreference = "Stop"

Remove-Item QuickLook.Plugin.AlphaVideoViewer.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path bin/Release -Exclude "*.pdb", "*.xml"
Compress-Archive -Path $files -DestinationPath "QuickLook.Plugin.AlphaVideoViewer.zip"
Move-Item "QuickLook.Plugin.AlphaVideoViewer.zip" "QuickLook.Plugin.AlphaVideoViewer.qlplugin"

Write-Host "Done: QuickLook.Plugin.AlphaVideoViewer.qlplugin" -ForegroundColor Green
