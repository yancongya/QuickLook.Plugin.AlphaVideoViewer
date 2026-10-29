# Update version from git tag
# Usage: powershell -File Scripts/update-version.ps1

$ErrorActionPreference = "Stop"

$tag = git describe --always --tags "--abbrev=0" 2>$null
if ($tag) { $tag = $tag.Trim() }
# 仓库还没有任何 tag 时，git describe --always 会回退成打印 commit hash——那不是版本号
if (-not $tag -or $tag -notmatch '^v?\d+(\.\d+){1,3}$') { $tag = "1.0.0" }

$revision = git describe --always --tags 2>$null
if (-not $revision) { $revision = "$tag-0-g$(git rev-parse --short HEAD)" }

$version = $tag.TrimStart('v')

$assemblyInfo = @"
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyTitle("QuickLook.Plugin.AlphaVideoViewer")]
[assembly: AssemblyDescription("QuickLook plugin for previewing live-stream gift/effect transparent videos (AlphaPlayer / Tencent VAP / YYEVA alpha-split MP4/MOV)")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("QuickLook.Plugin.AlphaVideoViewer")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
[assembly: AssemblyInformationalVersion("$revision")]
"@

$assemblyInfo | Out-File "$PSScriptRoot\..\Properties\AssemblyInfo.cs" -Encoding utf8

Write-Host "Version updated to: $version ($revision)" -ForegroundColor Green
