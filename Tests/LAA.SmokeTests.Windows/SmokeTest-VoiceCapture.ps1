<#
.SYNOPSIS
    LAA (Local AI Assistant) Windows Smoke Test for Voice Capture
.DESCRIPTION
    Launches the app, verifies the existence of the Voice Capture (microphone) button,
    and checks its visibility and state.
#>
param(
    [string] $ExePath        = "",
    [int]    $MaxWaitSeconds = 30,
    [switch] $KeepAppOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Load UI Automation ──────────────────────────────────────────────────────
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

$AE   = [System.Windows.Automation.AutomationElement]
$Tree = [System.Windows.Automation.TreeScope]

$script:Passed  = 0
$script:Failed  = 0
$script:Proc    = $null
$script:Window  = $null

# ─── Exe resolution ──────────────────────────────────────────────────────────
function Resolve-ExePath {
    if ($ExePath -and (Test-Path $ExePath)) { return $ExePath }

    $root = "C:\Users\benho\source\repos\LocalAIAssistant"
    $tfm  = "net9.0-windows10.0.19041.0"
    $candidates = @(
        "$root\bin\Debug\$tfm\win10-x64\LocalAIAssistant.Ui.Maui.exe"
        "$root\bin\Debug\$tfm\LocalAIAssistant.Ui.Maui.exe"
        "$root\bin\Release\$tfm\win10-x64\LocalAIAssistant.Ui.Maui.exe"
        "$root\bin\Release\$tfm\LocalAIAssistant.Ui.Maui.exe"
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($found) { return $found }
    throw "Could not find LocalAIAssistant.Ui.Maui.exe. Build with: dotnet build -f net9.0-windows10.0.19041.0"
}

# ─── UIA helpers ─────────────────────────────────────────────────────────────
function Find-ById {
    param($Root, [string] $AutomationId, [int] $TimeoutSeconds = 10)
    $cond     = New-Object System.Windows.Automation.PropertyCondition(
                    $AE::AutomationIdProperty, $AutomationId)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $el = $Root.FindFirst($Tree::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 300
    }
    return $null
}

function Get-ElementTreeText {
    param($Root, $MaxDepth = 3, $Depth = 0)
    if (-not $Root) { return "" }
    if ($Depth -gt $MaxDepth) { return "" }
    $indent = "  " * $Depth
    $id     = $Root.Current.AutomationId
    $name   = $Root.Current.Name
    $class  = $Root.Current.ClassName
    $line   = "$indent* [$class] Id='$id' Name='$name'"
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $child  = $walker.GetFirstChild($Root)
    $sb     = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine($line)
    while ($child) {
        [void]$sb.Append((Get-ElementTreeText $child $MaxDepth ($Depth + 1)))
        $child = $walker.GetNextSibling($child)
    }
    return $sb.ToString()
}

function Run-Test {
    param([string] $Name, [scriptblock] $Body)
    $detail = ""
    $status = "FAIL"
    try {
        $result = & $Body
        if ($result -eq $true) {
            $status = "PASS"
            $script:Passed++
        } else {
            $detail = if ($result -is [string]) { $result } else { "returned false" }
            $script:Failed++
            $detail += "`n       --- UIA tree at failure ---`n" + (Get-ElementTreeText $script:Window 3)
        }
    } catch {
        $detail  = $_.Exception.Message
        $script:Failed++
        try { $detail += "`n       --- UIA tree at failure ---`n" + (Get-ElementTreeText $script:Window 3) } catch { }
    }

    $icon   = if ($status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    $colour = if ($status -eq "PASS") { "Green" }  else { "Red" }
    Write-Host "$icon $Name" -ForegroundColor $colour
    if ($detail) { Write-Host $detail -ForegroundColor DarkGray }
}

# ─── App launch ──────────────────────────────────────────────────────────────
$resolvedExe = Resolve-ExePath
Write-Host ""
Write-Host "=== LAA Windows Voice Capture Smoke Test ===" -ForegroundColor Cyan
Write-Host "Exe: $resolvedExe"
Write-Host ""

# Clean up database to ensure isolated test data state
$dbFolders = @(
    "C:\Users\benho\AppData\Local\Data"
    "C:\Users\benho\AppData\Local\User Name\com.snikpoh.localaiassistant.debug\Data"
)
foreach ($folder in $dbFolders) {
    if (Test-Path $folder) {
        Get-ChildItem -Path $folder -Filter "localaiassistant.db*" | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Launching app..." -ForegroundColor DarkGray
$script:Proc = Start-Process -FilePath $resolvedExe -PassThru

Write-Host "Waiting for app window..." -ForegroundColor DarkGray
$deadline = (Get-Date).AddSeconds($MaxWaitSeconds)
$script:Window = $null
while ((Get-Date) -lt $deadline) {
    $script:Proc.Refresh()
    if ($script:Proc.HasExited) { break }
    $pidCond  = New-Object System.Windows.Automation.PropertyCondition(
                    $AE::ProcessIdProperty, $script:Proc.Id)
    $candidate = $AE::RootElement.FindFirst($Tree::Children, $pidCond)
    if ($candidate) {
        $script:Window = $candidate
        break
    }
    Start-Sleep -Milliseconds 500
}

if (-not $script:Window) {
    Write-Host "[FATAL] Main window not found. Exiting." -ForegroundColor Red
    exit 1
}

Write-Host "Waiting for Chat page to settle..." -ForegroundColor DarkGray
$readyEditor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds $MaxWaitSeconds
if (-not $readyEditor) {
    Write-Host "[FATAL] ChatEditor not found within $MaxWaitSeconds seconds." -ForegroundColor Red
    exit 1
}

# ─── Verification Tests ──────────────────────────────────────────────────────

Run-Test "Verify Voice Capture Button is visible on Chat page" -Body {
    $voiceBtn = Find-ById $script:Window "VoiceButton" -TimeoutSeconds 5
    if (-not $voiceBtn) { return "VoiceButton element not found in UI tree." }
    if ($voiceBtn.Current.IsOffscreen) { return "VoiceButton is offscreen or hidden." }
    return $true
}

# ─── Cleanup ─────────────────────────────────────────────────────────────────
if (-not $KeepAppOpen) {
    Write-Host "Closing application..." -ForegroundColor DarkGray
    if ($script:Proc -and -not $script:Proc.HasExited) {
        Stop-Process -Id $script:Proc.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "=== Results: $script:Passed passed, $script:Failed failed ===" -ForegroundColor $(if ($script:Failed -eq 0) { "Green" } else { "Red" })

exit $script:Failed
