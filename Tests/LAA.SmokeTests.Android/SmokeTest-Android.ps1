<#
.SYNOPSIS
    LAA (Local AI Assistant) Android Smoke Test Suite

.DESCRIPTION
    Exercises the main user flows of the LAA MAUI app on a connected Android device
    or emulator using adb + UIAutomator. Each test prints PASS/FAIL with a description
    and dumps the UI hierarchy on failure to aid diagnosis.

.PARAMETER Device
    The adb device serial (e.g. "emulator-5554"). Defaults to the first connected device.

.PARAMETER PackageName
    The Android package name. Auto-detected from installed packages if not supplied.

.PARAMETER MaxWaitSeconds
    How long to wait for the app to be ready before timing out. Default: 30.

.PARAMETER KeepAppOpen
    When set, the app is left running after all tests complete.

.EXAMPLE
    .\SmokeTest-Android.ps1
    .\SmokeTest-Android.ps1 -Device emulator-5554 -MaxWaitSeconds 45
#>
param(
    [string] $Device        = "",
    [string] $PackageName   = "",
    [int]    $MaxWaitSeconds = 30,
    [switch] $KeepAppOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Counters ───────────────────────────────────────────────────────────────
$script:Passed  = 0
$script:Failed  = 0
$script:Results = [System.Collections.Generic.List[object]]::new()

# ─── adb helpers ────────────────────────────────────────────────────────────

function Invoke-Adb {
    param([string[]] $Args)
    $adbArgs = if ($script:Device) { @("-s", $script:Device) + $Args } else { $Args }
    & adb @adbArgs 2>&1
}

function Get-UiDump {
    <#
    Captures a UIAutomator hierarchy XML.
    Returns $null and prints a warning when the dump times out or the file is empty.
    #>
    $remote  = "/sdcard/laa_smoke_dump.xml"
    $local   = Join-Path $env:TEMP "laa_smoke_dump.xml"

    Invoke-Adb "shell", "uiautomator", "dump", $remote | Out-Null
    Invoke-Adb "pull", $remote, $local | Out-Null

    if (-not (Test-Path $local)) { return $null }
    $content = Get-Content $local -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($content)) { return $null }

    try   { return [xml]$content }
    catch { Write-Warning "Failed to parse UI dump XML: $_"; return $null }
}

function Find-Node {
    <#
    Searches the UIAutomator dump for a node matching ANY supplied criteria.
    All supplied criteria must match (AND logic within a single node).
    #>
    param(
        [xml]    $Dump,
        [string] $Text        = $null,
        [string] $ContentDesc = $null,
        [string] $ClassName   = $null,
        [string] $ResourceId  = $null,
        [string] $Enabled     = $null
    )

    $xpathParts = @()
    if ($Text)        { $xpathParts += "@text='$Text'" }
    if ($ContentDesc) { $xpathParts += "@content-desc='$ContentDesc'" }
    if ($ClassName)   { $xpathParts += "@class='$ClassName'" }
    if ($ResourceId)  { $xpathParts += "@resource-id='$ResourceId'" }
    if ($Enabled)     { $xpathParts += "@enabled='$Enabled'" }

    if ($xpathParts.Count -eq 0) { return $null }
    $xpath = "//node[" + ($xpathParts -join " and ") + "]"

    try   { return $Dump.SelectSingleNode($xpath) }
    catch { return $null }
}

function Find-AllNodes {
    param([xml] $Dump, [string] $XPath)
    try   { return @($Dump.SelectNodes($XPath)) }
    catch { return @() }
}

function Get-NodeCenter {
    param($Node)
    if ($null -eq $Node) { return $null }
    $bounds = $Node.bounds   # "[x1,y1][x2,y2]"
    if ($bounds -match '\[(\d+),(\d+)\]\[(\d+),(\d+)\]') {
        return @{
            X = [int](([int]$Matches[1] + [int]$Matches[3]) / 2)
            Y = [int](([int]$Matches[2] + [int]$Matches[4]) / 2)
        }
    }
    return $null
}

function Tap-Node {
    param($Node, [int] $DelayMs = 700)
    $center = Get-NodeCenter $Node
    if ($null -eq $center) {
        Write-Warning "Tap-Node: could not determine bounds for node"
        return $false
    }
    Invoke-Adb "shell", "input", "tap", $center.X, $center.Y | Out-Null
    Start-Sleep -Milliseconds $DelayMs
    return $true
}

function Tap-Tab {
    <# Clicks a Shell tab by its visible title text. #>
    param([string] $Title, [int] $DelayMs = 1000)
    $dump = Get-UiDump
    if ($null -eq $dump) { return $false }

    # MAUI Shell bottom-nav tabs can appear with text OR content-desc matching the title.
    $node = Find-Node $dump -Text $Title
    if ($null -eq $node) {
        $node = Find-Node $dump -ContentDesc $Title
    }
    if ($null -eq $node) { return $false }
    Tap-Node $node -DelayMs $DelayMs | Out-Null
    return $true
}

function Wait-ForElement {
    <#
    Polls the UI dump until the predicate returns a non-null node or the timeout expires.
    Returns the node, or $null on timeout.
    #>
    param(
        [scriptblock] $Predicate,
        [int]         $TimeoutSeconds = $MaxWaitSeconds,
        [int]         $IntervalMs     = 1000
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $dump = Get-UiDump
        if ($dump) {
            $node = & $Predicate $dump
            if ($null -ne $node) { return $node }
        }
        Start-Sleep -Milliseconds $IntervalMs
    }
    return $null
}

# ─── Test runner ─────────────────────────────────────────────────────────────

function Run-Test {
    param([string] $Name, [scriptblock] $Body)

    $result = [PSCustomObject]@{ Name = $Name; Status = "FAIL"; Detail = "" }
    try {
        $outcome = & $Body
        if ($outcome -eq $true) {
            $result.Status = "PASS"
            $script:Passed++
        } else {
            $result.Detail = if ($outcome -is [string]) { $outcome } else { "Test body returned false" }
            $script:Failed++

            # Dump the UI hierarchy so the caller can see what was on screen.
            $dump = Get-UiDump
            if ($dump) {
                $dumpPath = Join-Path $env:TEMP "laa_failure_$(($Name -replace '\W','_')).xml"
                $dump.Save($dumpPath)
                $result.Detail += " | Hierarchy saved: $dumpPath"
            }
        }
    } catch {
        $result.Detail = $_.Exception.Message
        $script:Failed++

        $dump = Get-UiDump
        if ($dump) {
            $dumpPath = Join-Path $env:TEMP "laa_failure_$(($Name -replace '\W','_')).xml"
            $dump.Save($dumpPath)
            $result.Detail += " | Hierarchy saved: $dumpPath"
        }
    }

    $icon   = if ($result.Status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    $colour = if ($result.Status -eq "PASS") { "Green" }  else { "Red" }
    Write-Host "$icon $Name" -ForegroundColor $colour
    if ($result.Detail) {
        Write-Host "       $($result.Detail)" -ForegroundColor DarkGray
    }

    $script:Results.Add($result)
}

# ─── Device / package setup ──────────────────────────────────────────────────

function Resolve-Device {
    $devices = (& adb devices 2>&1) | Select-Object -Skip 1 |
               Where-Object { $_ -match "^\S+\s+device$" } |
               ForEach-Object { ($_ -split "\s+")[0] }

    if (-not $devices) {
        throw "No Android device or emulator connected. Run 'adb devices' to check."
    }
    return $devices[0]
}

function Resolve-PackageName {
    $packages = Invoke-Adb "shell", "pm", "list", "packages" |
                Where-Object { $_ -match "localaiassistant" } |
                ForEach-Object { $_ -replace "^package:", "" }

    if (-not $packages) {
        throw "LAA package not found on device. Is the app installed?"
    }
    # Prefer the debug/dev variant if multiple are installed.
    $preferred = $packages | Where-Object { $_ -match "\.debug" } | Select-Object -First 1
    if ($null -ne $preferred) { return $preferred }
    return ($packages | Select-Object -First 1)
}

# ─── Initialise ──────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== LAA Android Smoke Tests ===" -ForegroundColor Cyan
Write-Host ""

if (-not $Device) { $script:Device = Resolve-Device } else { $script:Device = $Device }
Write-Host "Device:  $($script:Device)"

if (-not $PackageName) { $PackageName = Resolve-PackageName }
Write-Host "Package: $PackageName"
Write-Host ""

# ─── Launch app ──────────────────────────────────────────────────────────────

Write-Host "Launching app..." -ForegroundColor DarkGray
# Force-stop any stale instance first.
Invoke-Adb "shell", "am", "force-stop", $PackageName | Out-Null
Start-Sleep -Milliseconds 400

# Launch via monkey (resolves the launcher activity automatically).
Invoke-Adb "shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1" | Out-Null

Write-Host "Waiting for app to be ready..." -ForegroundColor DarkGray
$chatEditor = Wait-ForElement -TimeoutSeconds $MaxWaitSeconds -Predicate {
    param($dump)
    # Chat page is ready when the EditText (chat editor) is visible.
    Find-Node $dump -ClassName "android.widget.EditText"
}

if ($null -eq $chatEditor) {
    Write-Host ""
    Write-Host "[FATAL] App did not reach the Chat page within $MaxWaitSeconds seconds." -ForegroundColor Red
    Write-Host "        Check that the app is installed and the correct package name is used." -ForegroundColor Red
    exit 1
}
Write-Host "App ready." -ForegroundColor DarkGray
Write-Host ""

# ─── Smoke Tests ─────────────────────────────────────────────────────────────

# ── 1. App launches and Chat page is visible ─────────────────────────────────
Run-Test "App launches and Chat page is visible" {
    $dump   = Get-UiDump
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    $null -ne $editor
}

# ── 2. Chat editor is focusable and clickable ────────────────────────────────
# Regression guard for the Android Editor tap-to-focus bug fixed in PRs #23-#28.
Run-Test "Chat editor is focusable and clickable" {
    $dump   = Get-UiDump
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    if ($null -eq $editor) { return "EditText not found in hierarchy" }
    if ($editor.focusable -ne "true")  { return "Editor is not focusable (focusable=false)" }
    if ($editor.clickable -ne "true")  { return "Editor is not clickable (clickable=false)" }
    $true
}

# ── 3. Editor enabled attribute is true (not blocked by an overlay) ──────────
Run-Test "Chat editor is enabled (no overlay blocking input)" {
    $dump   = Get-UiDump
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    if ($null -eq $editor) { return "EditText not found" }
    if ($editor.enabled -ne "true") { return "Editor disabled - possible overlay or permission dialog intercepting touches" }
    $true
}

# ── 4. Can type a message and Send button is present ─────────────────────────
Run-Test "Can type a message and Send button is present" {
    # Tap editor to focus it.
    $dump   = Get-UiDump
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    if ($null -eq $editor) { return "EditText not found" }
    Tap-Node $editor | Out-Null
    Start-Sleep -Milliseconds 300

    # Type a test message.
    Invoke-Adb "shell", "input", "text", "smoke_test_hello" | Out-Null
    Start-Sleep -Milliseconds 600

    # Verify text appeared and Send button exists.
    $dump2   = Get-UiDump
    $editor2 = Find-Node $dump2 -ClassName "android.widget.EditText"
    $send    = Find-Node $dump2 -Text "Send"

    if ($null -eq $editor2) { return "EditText disappeared after typing" }
    if ([string]::IsNullOrEmpty($editor2.text)) { return "Editor text is empty after typing - input may not have reached the field" }
    if ($null -eq $send) { return "Send button not found after typing" }

    # Clear the editor so subsequent tests start clean.
    Invoke-Adb "shell", "input", "keyevent", "KEYCODE_CTRL_A" | Out-Null
    Invoke-Adb "shell", "input", "keyevent", "KEYCODE_DEL"     | Out-Null
    Start-Sleep -Milliseconds 300
    $true
}

# ── 5. Navigation to Chats tab works ─────────────────────────────────────────
Run-Test "Navigation to Chats tab works" {
    $ok = Tap-Tab "Chats" -DelayMs 1200
    if (-not $ok) { return "Could not find 'Chats' tab in the bottom navigation bar" }

    $dump = Get-UiDump
    # Chats page shows "Past Conversations" label OR the New Chat button.
    $header = Find-Node $dump -Text "Past Conversations"
    $newBtn = Find-Node $dump -Text "New Chat"
    if ($null -eq $header -and $null -eq $newBtn) {
        return "Neither 'Past Conversations' header nor 'New Chat' button found after switching to Chats tab"
    }
    $true
}

# ── 6. Chats page shows list or empty state ───────────────────────────────────
Run-Test "Chats page shows conversation list or empty state" {
    $dump     = Get-UiDump
    $list     = Find-Node $dump -Text "Past Conversations"
    $empty    = Find-Node $dump -Text "No past conversations"
    $newChat  = Find-Node $dump -Text "New Chat"
    if ($null -eq $list -and $null -eq $empty -and $null -eq $newChat) {
        return "Chats page content not recognised - expected list header, empty state, or New Chat button"
    }
    $true
}

# ── 7. New Chat button is present on Chats page ──────────────────────────────
Run-Test "New Chat button is present on Chats page" {
    $dump   = Get-UiDump
    $newBtn = Find-Node $dump -Text "New Chat"
    if ($null -eq $newBtn) { return "'New Chat' button not found on Chats page" }
    if ($newBtn.enabled -ne "true") { return "'New Chat' button found but is not enabled" }
    $true
}

# ── 8. Navigation to Inbox tab works ─────────────────────────────────────────
Run-Test "Navigation to Inbox tab works" {
    $ok = Tap-Tab "Inbox" -DelayMs 1200
    if (-not $ok) { return "Could not find 'Inbox' tab in the bottom navigation bar" }

    $dump     = Get-UiDump
    # Inbox page title is "Knowledge"; look for it or the CollectionView content area.
    $title    = Find-Node $dump -Text "Knowledge"
    $offline  = Find-Node $dump -Text "Offline - showing last synced data and queued items"
    $anyGroup = Find-AllNodes $dump "//node[@class='androidx.recyclerview.widget.RecyclerView']"

    if ($null -eq $title -and $null -eq $offline -and $anyGroup.Count -eq 0) {
        return "Inbox page content not recognised after switching tab"
    }
    $true
}

# ── 9. Inbox page loads (items or empty) ─────────────────────────────────────
Run-Test "Inbox page loads without crashing" {
    # Give it a moment if it was loading.
    Start-Sleep -Milliseconds 800
    $dump = Get-UiDump
    if ($null -eq $dump) { return "UI dump returned null - app may have crashed" }
    # Any non-empty hierarchy means the page is alive.
    $root = $dump.SelectSingleNode("//hierarchy")
    $null -ne $root
}

# ── 10. Navigate back to Chat tab ────────────────────────────────────────────
Run-Test "Navigate back to Chat tab from Inbox" {
    $ok = Tap-Tab "Chat" -DelayMs 1200
    if (-not $ok) { return "Could not find 'Chat' tab in the bottom navigation bar" }

    $dump   = Get-UiDump
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    if ($null -eq $editor) { return "Chat editor not visible after navigating back to Chat tab" }
    $true
}

# ── 11. App survives rapid tab cycling ───────────────────────────────────────
Run-Test "App survives rapid tab cycling without crashing" {
    $tabs = @("Chats", "Inbox", "Memory", "Logs", "Settings", "Chat")
    foreach ($tab in $tabs) {
        $ok = Tap-Tab $tab -DelayMs 600
        if (-not $ok) {
            Write-Warning "Tab '$tab' not found during rapid cycling - skipping remaining tabs"
            break
        }
    }
    Start-Sleep -Milliseconds 800
    $dump   = Get-UiDump
    if ($null -eq $dump) { return "UI dump returned null after rapid tab cycling - app may have crashed" }
    $editor = Find-Node $dump -ClassName "android.widget.EditText"
    if ($null -eq $editor) { return "Chat editor not visible after rapid tab cycling" }
    $true
}

# ── 12. Back navigation from Chats doesn't crash ─────────────────────────────
Run-Test "Back navigation from Chats page does not crash" {
    Tap-Tab "Chats" -DelayMs 1000 | Out-Null
    Invoke-Adb "shell", "input", "keyevent", "KEYCODE_BACK" | Out-Null
    Start-Sleep -Milliseconds 800

    $dump = Get-UiDump
    if ($null -eq $dump) { return "UI dump returned null after back navigation - app may have crashed" }
    $true
}

# ── 13. Ask Coco toggle is NOT shown on Android (WinUI-only isolation guard) ──
Run-Test "Ask Coco toolbar toggle is absent on Android (Windows-only feature)" {
    Tap-Tab "Chat" -DelayMs 1200 | Out-Null

    $dump = Get-UiDump
    if ($null -eq $dump) { return "UI dump returned null" }

    # IsCocoToggleVisible requires WinUI platform — the element must not appear on Android.
    $cocoNode = Find-Node $dump -Text "Ask Coco"
    if ($null -ne $cocoNode) {
        return "Ask Coco toolbar node was found on Android — it should be hidden (WinUI-only)"
    }
    $true
}

# ── 14. Settings page loads without crash when Coco section is hidden ─────────
Run-Test "Settings page loads cleanly on Android (Coco section correctly hidden)" {
    $ok = Tap-Tab "Settings" -DelayMs 1500
    if (-not $ok) { return "Could not navigate to Settings tab" }

    $dump = Get-UiDump
    if ($null -eq $dump) { return "UI dump returned null after navigating to Settings" }

    # On Android, IsCocoSectionVisible=false — the Coco section must not appear.
    $cocoSection = Find-Node $dump -Text "COCO — CODE INTELLIGENCE"
    if ($null -ne $cocoSection) {
        return "Coco section header was found on Android — it should be hidden (WinUI-only)"
    }

    # The Settings page must still render without crashing.
    $root = $dump.SelectSingleNode("//hierarchy")
    if ($null -eq $root) { return "Settings hierarchy not found — app may have crashed" }

    # Navigate back to Chat to leave app in clean state.
    Tap-Tab "Chat" -DelayMs 1000 | Out-Null
    $true
}

# ─── Teardown ────────────────────────────────────────────────────────────────

if (-not $KeepAppOpen) {
    Invoke-Adb "shell", "am", "force-stop", $PackageName | Out-Null
}

# ─── Summary ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "Results: $($script:Passed) passed, $($script:Failed) failed" -ForegroundColor $(
    if ($script:Failed -eq 0) { "Green" } else { "Yellow" }
)
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""

if ($script:Failed -gt 0) { exit 1 } else { exit 0 }
