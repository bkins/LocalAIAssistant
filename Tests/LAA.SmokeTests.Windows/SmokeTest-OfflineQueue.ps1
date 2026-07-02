<#
.SYNOPSIS
    LAA (Local AI Assistant) Windows Smoke Test for Offline Queue
.DESCRIPTION
    Launches the app in offline state (by assuming the backend API is not running),
    types a message, sends it, and verifies it enqueues correctly.
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

function Find-ByName {
    param($Root, [string] $Name, [int] $TimeoutSeconds = 8)
    $cond     = New-Object System.Windows.Automation.PropertyCondition(
                    $AE::NameProperty, $Name)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $el = $Root.FindFirst($Tree::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 300
    }
    return $null
}

function Invoke-Element {
    param($Element)
    try {
        $pat = $Element.GetCurrentPattern(
                   [System.Windows.Automation.InvokePattern]::Pattern)
        $pat.Invoke()
        return $true
    } catch { }
    try {
        $pat = $Element.GetCurrentPattern(
                   [System.Windows.Automation.SelectionItemPattern]::Pattern)
        $pat.Select()
        return $true
    } catch { }
    return $false
}

function Set-ElementValue {
    param($Element, $Text)
    try {
        $pat = $Element.GetCurrentPattern(
                   [System.Windows.Automation.ValuePattern]::Pattern)
        $pat.SetValue($Text)
        return $true
    } catch { }
    try {
        $Element.SetFocus()
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait($Text)
        return $true
    } catch { }
    return $false
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

# Dot-source the common helpers
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$helpersPath = Join-Path $scriptDir "..\Test-Helpers.ps1"
if (-not (Test-Path $helpersPath)) {
    $helpersPath = Join-Path $scriptDir "Test-Helpers.ps1"
}
. $helpersPath

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
            try { Take-WindowsScreenshot -FileNamePrefix "LAA_OfflineQueue_Failure" -ProcessId $script:Proc.Id } catch { }
            $detail += "`n       --- UIA tree at failure ---`n" + (Get-ElementTreeText $script:Window 3)
        }
    } catch {
        $detail  = $_.Exception.Message
        $script:Failed++
        try { Take-WindowsScreenshot -FileNamePrefix "LAA_OfflineQueue_Failure" -ProcessId $script:Proc.Id } catch { }
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
Write-Host "=== LAA Windows Offline Queue Smoke Test ===" -ForegroundColor Cyan
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
Write-Host "App ready." -ForegroundColor DarkGray
try {
    $wshell = New-Object -ComObject WScript.Shell
    $wshell.AppActivate($script:Proc.Id) | Out-Null
    Start-Sleep -Milliseconds 600
} catch {}

# ─── Smoke Tests ─────────────────────────────────────────────────────────────

# Test 1: Send message in offline mode & verify offline queue confirmation
Run-Test "Send message in offline mode and verify enqueued state" {
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 5
    if (-not $editor) { return "ChatEditor not found" }

    $ok = Set-ElementValue $editor "Offline Queue Smoke Test Message"
    if (-not $ok) { return "Could not set value in ChatEditor" }
    Start-Sleep -Milliseconds 300

    $send = Find-ById $script:Window "SendButton" -TimeoutSeconds 5
    if (-not $send) { return "SendButton not found" }

    $ok = Invoke-Element $send
    if (-not $ok) { return "Could not click SendButton" }
    Start-Sleep -Seconds 2 # Give it time to hit the health check, fail, and update UI

    # Verify that the assistant bubble shows that the message was saved to the offline queue
    $bubble = Find-ByName $script:Window "Message saved to offline queue (currently offline)." -TimeoutSeconds 8
    if (-not $bubble) {
        $bubble = Find-ByName $script:Window "Connection lost. Message saved to offline queue (will replay when online)." -TimeoutSeconds 2
    }
    if (-not $bubble) {
        return "Offline queue response bubble not found in chat UI"
    }

    # Verify the Offline Queue badge count increases to 1 in the shell
    $badge = Find-ById $script:Window "OfflineQueueBadgeLabel" -TimeoutSeconds 5
    if (-not $badge) {
        return "Offline queue pending count badge 'OfflineQueueBadgeLabel' not found in UIA tree"
    }
    if ($badge.Current.Name -ne "1") {
        return "Offline queue pending count badge exists but text is '$($badge.Current.Name)' instead of '1'"
    }

    # Verify database table OfflineQueue has the record
    $dbRows = Query-SqliteDatabase "SELECT Input FROM OfflineQueue;"
    if (-not $dbRows) {
        return "No records found in SQLite database OfflineQueue table"
    }
    if ($dbRows -notcontains "Offline Queue Smoke Test Message") {
        return "The enqueued message 'Offline Queue Smoke Test Message' was not found in SQLite Database (Rows: $dbRows)"
    }

    $true
}

# ─── Teardown ────────────────────────────────────────────────────────────────
if (-not $KeepAppOpen -and $script:Proc -and -not $script:Proc.HasExited) {
    $script:Proc.CloseMainWindow() | Out-Null
    $script:Proc.WaitForExit(3000) | Out-Null
    if (-not $script:Proc.HasExited) { $script:Proc.Kill() }
}

Write-Host ""
Write-Host "----------------------------------------------" -ForegroundColor DarkGray
$colour = if ($script:Failed -eq 0) { "Green" } else { "Yellow" }
Write-Host "Results: $($script:Passed) passed  /  $($script:Failed) failed" -ForegroundColor $colour
Write-Host "----------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

if ($script:Failed -gt 0) { exit 1 } else { exit 0 }
