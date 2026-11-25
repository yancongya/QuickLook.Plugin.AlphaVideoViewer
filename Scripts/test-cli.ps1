param(
    [string]$Sample = "",
    [int]$TimeoutSeconds = 12,
    [switch]$VerifyFeatures,
    [switch]$ShowWindow,
    [string]$ScreenshotPath = ""
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$repoRoot = Split-Path -Parent $PSScriptRoot
$binDir = Join-Path $repoRoot 'bin\Release'
if (-not (Test-Path (Join-Path $binDir 'QuickLook.Plugin.AlphaVideoViewer.dll'))) {
    throw "Build output not found in $binDir. Run Scripts/build.ps1 first."
}

if ([string]::IsNullOrWhiteSpace($Sample)) {
    $Sample = Join-Path $repoRoot 'Samples\vap_demo.mp4'
}
$Sample = (Resolve-Path $Sample).Path

$origTemp = $env:TEMP
$origTmp = $env:TMP
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("alpha-cli-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$env:TEMP = $tempDir
$env:TMP = $tempDir

if ($VerifyFeatures) {
    $tempSample = Join-Path $tempDir (Split-Path -Leaf $Sample)
    Copy-Item -LiteralPath $Sample -Destination $tempSample -Force
    $Sample = $tempSample
}

try {
    [System.Reflection.Assembly]::LoadFrom((Join-Path $binDir 'QuickLook.Common.dll')) | Out-Null
    [System.Reflection.Assembly]::LoadFrom((Join-Path $binDir 'Microsoft.Web.WebView2.Core.dll')) | Out-Null
    [System.Reflection.Assembly]::LoadFrom((Join-Path $binDir 'Microsoft.Web.WebView2.Wpf.dll')) | Out-Null
    [System.Reflection.Assembly]::LoadFrom((Join-Path $binDir 'QuickLook.Plugin.AlphaVideoViewer.dll')) | Out-Null

    $ctx = New-Object QuickLook.Common.Plugin.ContextObject
    $plugin = New-Object QuickLook.Plugin.AlphaVideoViewer.Plugin

    $canHandle = $plugin.CanHandle($Sample)
    Write-Host ("Sample     : " + $Sample)
    Write-Host ("CanHandle  : " + $canHandle)
    if (-not $canHandle) {
        throw "Plugin.CanHandle returned false for $Sample"
    }

    $plugin.Prepare($Sample, $ctx)
    Write-Host ("Size       : " + $ctx.PreferredSize)
    $plugin.View($Sample, $ctx)

    $panel = $ctx.ViewerContent
    $script:wv = $panel.FindName('WebView')
    $txt = $panel.FindName('TxtStatus')

    $script:loaded = $false
    $script:errMsg = $null
    $script:verifyResult = $null
    $script:verifyStarted = $false

    $script:win = New-Object System.Windows.Window
    $win = $script:win
    $win.Title = "AlphaVideoViewer CLI Test - " + (Split-Path -Leaf $Sample)
    $win.Width = 800
    $win.Height = 600
    $win.ShowInTaskbar = [bool]$ShowWindow
    if (-not $ShowWindow) {
        $win.WindowStyle = 'None'
        $win.ShowActivated = $false
        $win.Opacity = 0.01
        $win.Left = -32000
        $win.Top = -32000
    }
    $win.Content = $panel

    $verifyJs = @'
(async function() {
    var sleep = function(ms) { return new Promise(function(r) { setTimeout(r, ms); }); };
    var out = { ok: true, checks: {} };
    try {
        var v = document.getElementById('srcVideo');
        // 1. Speed cycling: 1x -> 1.5x -> 2x -> 0.5x -> 1x
        var btnSpeed = document.getElementById('btnSpeed');
        var rates = [];
        for (var i = 0; i < 4; i++) {
            btnSpeed.click();
            rates.push(v.playbackRate);
        }
        out.checks.speed = (rates.join(',') === '1.5,2,0.5,1');

        // 2. Frame stepping
        document.getElementById('btnNextFrame').click();
        await sleep(120);
        var t1 = v.currentTime;
        var pausedAfterStep = v.paused;
        document.getElementById('btnPrevFrame').click();
        await sleep(120);
        var t2 = v.currentTime;
        out.checks.frameStep = pausedAfterStep && (t1 > 0) && (t2 < t1);

        // 3. PerfOverlay & Metadata & Chart
        document.getElementById('btnMeta').click();
        await sleep(100);
        var overlay = document.getElementById('perfOverlay');
        var gridText = document.getElementById('perfMetaGrid').textContent || '';
        var fpsText = document.getElementById('valFps').textContent || '';
        out.checks.perfOverlay = overlay.classList.contains('show') && gridText.length > 10 && fpsText !== '-';

        // 4. Alpha-only mask mode
        var alphaOnlyItem = document.querySelector('#modeMenu .mode-item[data-mode="alphaOnly"]');
        alphaOnlyItem.click();
        await sleep(200);
        out.checks.alphaOnly = alphaOnlyItem.classList.contains('checked');

        // Switch back to alpha composite before PNG export
        document.querySelector('#modeMenu .mode-item[data-mode="alpha"]').click();
        await sleep(200);

        // 5. PNG Save + Clipboard Copy
        document.getElementById('btnSave').click();
        await sleep(150);
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'c', ctrlKey: true, bubbles: true }));
        await sleep(150);
        out.checks.captureDispatched = true;

        out.ok = out.checks.speed && out.checks.frameStep && out.checks.perfOverlay && out.checks.alphaOnly && out.checks.captureDispatched;
    } catch (e) {
        out.ok = false;
        out.error = String(e);
    }
    window.chrome.webview.postMessage({ type: 'cliVerify', result: out });
})();
'@

    $script:screenshotPath = $ScreenshotPath
    $wv.add_CoreWebView2InitializationCompleted({
        param($s, $e)
        if (-not $e.IsSuccess) {
            $script:errMsg = "WebView2 init failed"
            $script:win.Close()
            return
        }
        $script:wv.CoreWebView2.add_WebMessageReceived({
            param($s2, $e2)
            $raw = $e2.WebMessageAsJson
            Write-Host ("WebMsg     : " + $raw)
            if ($raw -match '"type"\s*:\s*"loaded"') { $script:loaded = $true }
            if ($raw -match '"type"\s*:\s*"error"') {
                $script:errMsg = $raw
                $script:win.Close()
            }
            if ($raw -match '"type"\s*:\s*"cliVerify"') {
                $script:verifyResult = $raw
                if (-not [string]::IsNullOrWhiteSpace($script:screenshotPath)) {
                    $script:capStream = New-Object System.IO.FileStream($script:screenshotPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
                    $capTask = $script:wv.CoreWebView2.CapturePreviewAsync([Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat]::Png, $script:capStream)
                    $capTask.GetAwaiter().OnCompleted([Action]{
                        $script:capStream.Dispose()
                        $script:win.Close()
                    })
                } else {
                    $script:win.Close()
                }
            }
            if ($raw -match 'auto-detect:|vapc metadata:') {
                if ($VerifyFeatures -and -not $script:verifyStarted) {
                    $script:verifyStarted = $true
                    $script:delayTimer = New-Object System.Windows.Threading.DispatcherTimer
                    $script:delayTimer.Interval = [TimeSpan]::FromMilliseconds(700)
                    $script:delayTimer.Add_Tick({
                        $script:delayTimer.Stop()
                        $script:wv.CoreWebView2.ExecuteScriptAsync($verifyJs) | Out-Null
                    })
                    $script:delayTimer.Start()
                } elseif (-not $VerifyFeatures -and -not [string]::IsNullOrWhiteSpace($script:screenshotPath)) {
                    if (-not $script:verifyStarted) {
                        $script:verifyStarted = $true
                        $script:delayTimer = New-Object System.Windows.Threading.DispatcherTimer
                        $script:delayTimer.Interval = [TimeSpan]::FromMilliseconds(800)
                        $script:delayTimer.Add_Tick({
                            $script:delayTimer.Stop()
                            $script:wv.CoreWebView2.ExecuteScriptAsync("document.getElementById('btnMeta').click();") | Out-Null
                            $script:capTimer = New-Object System.Windows.Threading.DispatcherTimer
                            $script:capTimer.Interval = [TimeSpan]::FromMilliseconds(400)
                            $script:capTimer.Add_Tick({
                                $script:capTimer.Stop()
                                $script:capStream = New-Object System.IO.FileStream($script:screenshotPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
                                $capTask = $script:wv.CoreWebView2.CapturePreviewAsync([Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat]::Png, $script:capStream)
                                $capTask.GetAwaiter().OnCompleted([Action]{
                                    $script:capStream.Dispose()
                                    $script:win.Close()
                                })
                            })
                            $script:capTimer.Start()
                        })
                        $script:delayTimer.Start()
                    }
                } elseif (-not $VerifyFeatures) {
                    $script:win.Close()
                }
            }
        })
    })

    $timer = New-Object System.Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromSeconds($TimeoutSeconds)
    $timer.Add_Tick({
        $timer.Stop()
        $win.Close()
    })
    $timer.Start()

    $win.ShowDialog() | Out-Null
    $timer.Stop()
    $plugin.Cleanup()

    Write-Host ("TxtStatus  : Visibility=" + $txt.Visibility + " Text=" + $txt.Text)
    if ($script:errMsg) {
        throw ("CLI test failed: " + $script:errMsg)
    }
    if (-not $script:loaded) {
        throw ("CLI test timed out after " + $TimeoutSeconds + "s without loaded message.")
    }

    if ($VerifyFeatures) {
        if (-not $script:verifyResult) {
            throw "Feature verification did not complete before timeout."
        }
        if ($script:verifyResult -notmatch '"ok"\s*:\s*true') {
            throw ("Feature verification failed: " + $script:verifyResult)
        }
        $pngs = @(Get-ChildItem -LiteralPath $tempDir -Filter "*.png")
        Write-Host ("ExportedPNG: " + $pngs.Count + " file(s)")
        if ($pngs.Count -lt 1) {
            throw "Expected exported PNG file in $tempDir, found none."
        }
        $bytes = [System.IO.File]::ReadAllBytes($pngs[0].FullName)
        if ($bytes.Length -lt 16 -or $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47) {
            throw "Exported file is not a valid PNG signature."
        }
        $hasClip = [System.Windows.Clipboard]::ContainsFileDropList()
        Write-Host ("Clipboard  : ContainsFileDropList=" + $hasClip)
        if (-not $hasClip) {
            throw "Expected Clipboard to contain PNG file drop list after Ctrl+C."
        }
    }

    Write-Host "CLI test PASSED."
}
finally {
    $env:TEMP = $origTemp
    $env:TMP = $origTmp
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
