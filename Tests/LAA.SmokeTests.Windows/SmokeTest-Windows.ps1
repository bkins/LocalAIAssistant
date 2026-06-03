<#
.SYNOPSIS
    LAA (Local AI Assistant) Windows Smoke Test Suite

.DESCRIPTION
    Exercises the main user flows of the LAA MAUI Windows app using the built-in
    Windows UI Automation (UIA3) API.  No external tools or servers required.

.PARAMETER ExePath
    Full path to LocalAIAssistant.Ui.Maui.exe.
    Auto-detected from the standard build output if not supplied.

.PARAMETER MaxWaitSeconds
    How long to wait for the app to be ready. Default: 30.

.PARAMETER KeepAppOpen
    Leave the app running after all tests complete.

.EXAMPLE
    .\SmokeTest-Windows.ps1
    .\SmokeTest-Windows.ps1 -ExePath "C:\...\LocalAIAssistant.Ui.Maui.exe"
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
Add-Type -AssemblyName System.Windows.Forms   # for SendKeys fallback

# Short aliases for the verbose UIA type names
$AE   = [System.Windows.Automation.AutomationElement]
$Tree = [System.Windows.Automation.TreeScope]

# ─── Counters ────────────────────────────────────────────────────────────────
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
    <# Polls for an element by AutomationId. Returns $null on timeout. #>
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
    <# Polls for an element by UIA Name property. Returns $null on timeout. #>
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

function Find-AllByClass {
    param($Root, [string] $ClassName)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
                $AE::ClassNameProperty, $ClassName)
    return @($Root.FindAll($Tree::Descendants, $cond))
}

function Invoke-Element {
    <# Clicks/invokes an element using InvokePattern, then SelectionItem as fallback. #>
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
    # Last resort: mouse click at the element's clickable point
    try {
        $pt = $Element.GetClickablePoint()
        [System.Windows.Forms.Cursor]::Position = [System.Drawing.Point]::new(
            [int]$pt.X, [int]$pt.Y)
        Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class UiaMouse {
    [DllImport("user32.dll")] public static extern void mouse_event(uint f,int x,int y,uint d,UIntPtr e);
    public const uint DOWN=2,UP=4;
    public static void Click(){mouse_event(DOWN,0,0,0,UIntPtr.Zero);System.Threading.Thread.Sleep(50);mouse_event(UP,0,0,0,UIntPtr.Zero);}
}
"@ -ErrorAction SilentlyContinue
        [UiaMouse]::Click()
        return $true
    } catch { }
    return $false
}

function Set-ElementValue {
    <# Types text into an element via ValuePattern or keyboard fallback. #>
    param($Element, [string] $Text)
    try {
        $pat = $Element.GetCurrentPattern(
                   [System.Windows.Automation.ValuePattern]::Pattern)
        $pat.SetValue($Text)
        return $true
    } catch { }
    # Keyboard fallback: focus + send keys
    try {
        $Element.SetFocus()
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait($Text)
        return $true
    } catch { }
    return $false
}

function Clear-ElementValue {
    param($Element)
    try {
        $pat = $Element.GetCurrentPattern(
                   [System.Windows.Automation.ValuePattern]::Pattern)
        $pat.SetValue("")
        return $true
    } catch { }
    try {
        $Element.SetFocus()
        Start-Sleep -Milliseconds 150
        [System.Windows.Forms.SendKeys]::SendWait("^a{DELETE}")
        return $true
    } catch { }
    return $false
}

function Click-Tab {
    <# Navigates to a Shell tab by its visible title. #>
    param([string] $Title, [int] $DelayMs = 800)
    # Tabs in MAUI Shell on Windows can appear as NavigationViewItem (Name = title)
    # or as a list item whose Name matches the tab title.
    $tab = Find-ByName $script:Window $Title -TimeoutSeconds 5
    if (-not $tab) { return $false }
    Invoke-Element $tab | Out-Null
    Start-Sleep -Milliseconds $DelayMs
    return $true
}

function Get-ElementTreeText {
    <# Returns a condensed text representation of the UIA subtree (for failure output). #>
    param($Root, [int] $MaxDepth = 4, [int] $Depth = 0)
    if ($Depth -gt $MaxDepth) { return "" }
    $indent = "  " * $Depth
    $name   = $Root.Current.Name
    $id     = $Root.Current.AutomationId
    $class  = $Root.Current.ClassName
    $line   = "$indent[$class] Name='$name' Id='$id'"
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

# ─── Test runner ─────────────────────────────────────────────────────────────

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
Write-Host "=== LAA Windows Smoke Tests ===" -ForegroundColor Cyan
Write-Host "Exe: $resolvedExe"
Write-Host ""

Write-Host "Launching app..." -ForegroundColor DarkGray
$script:Proc = Start-Process -FilePath $resolvedExe -PassThru

# Wait for the main window to appear in the UIA tree.
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
        # Give MAUI a moment to render its full hierarchy before we start querying.
        Start-Sleep -Seconds 2
        $script:Window = $candidate
        break
    }
    Start-Sleep -Milliseconds 600
}

if (-not $script:Window) {
    if ($script:Proc.HasExited) {
        Write-Host "[FATAL] App process exited immediately (exit code $($script:Proc.ExitCode)). Check the build." -ForegroundColor Red
    } else {
        Write-Host "[FATAL] App window not found in UIA tree after $MaxWaitSeconds seconds." -ForegroundColor Red
    }
    exit 1
}

# Wait for the Chat editor to be ready, confirming MAUI navigation has settled.
Write-Host "Waiting for Chat page to settle..." -ForegroundColor DarkGray
$readyEditor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds $MaxWaitSeconds
if (-not $readyEditor) {
    Write-Host ""
    Write-Host "[FATAL] ChatEditor not found within $MaxWaitSeconds seconds." -ForegroundColor Yellow
    Write-Host "        (App is running -- dumping visible UIA tree for diagnosis)" -ForegroundColor Yellow
    Write-Host (Get-ElementTreeText $script:Window 4) -ForegroundColor DarkGray
    # Continue anyway so other tests can collect data.
}
Write-Host "App ready." -ForegroundColor DarkGray
Write-Host ""

# ─── Smoke Tests ─────────────────────────────────────────────────────────────

# ── 1. Chat page visible ──────────────────────────────────────────────────────
Run-Test "App launches and Chat page is visible" {
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 8
    if (-not $editor) { return "ChatEditor (AutomationId) not found in UIA tree" }
    if ($editor.Current.IsOffscreen) { return "ChatEditor found but is offscreen" }
    $true
}

# ── 2. Chat editor enabled (regression guard for overlay bug) ─────────────────
Run-Test "Chat editor is enabled and not blocked by an overlay" {
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 5
    if (-not $editor) { return "ChatEditor not found" }
    if (-not $editor.Current.IsEnabled) { return "ChatEditor.IsEnabled = false" }
    $true
}

# ── 3. Chat editor accepts text input ─────────────────────────────────────────
Run-Test "Chat editor accepts text input" {
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 5
    if (-not $editor) { return "ChatEditor not found" }
    $ok = Set-ElementValue $editor "smoke_test_hello"
    Start-Sleep -Milliseconds 400
    if (-not $ok) { return "Could not set text via ValuePattern or SendKeys" }

    # Read back the value to confirm it landed.
    try {
        $vp  = $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        $val = $vp.Current.Value
        if ([string]::IsNullOrEmpty($val)) { return "Editor value empty after SetValue - text did not register" }
    } catch { } # Some MAUI builds don't expose ValuePattern for read; skip read-back check.

    # Clean up.
    Clear-ElementValue $editor | Out-Null
    $true
}

# ── 4. Send button present and enabled ────────────────────────────────────────
Run-Test "Send button is present and enabled" {
    $send = Find-ById $script:Window "SendButton" -TimeoutSeconds 5
    if (-not $send) { return "SendButton not found in UIA tree" }
    if (-not $send.Current.IsEnabled) { return "SendButton.IsEnabled = false" }
    if ($send.Current.IsOffscreen)    { return "SendButton is offscreen" }
    $true
}

# ── 5. Clear button present ───────────────────────────────────────────────────
Run-Test "Clear button is present and enabled" {
    $clear = Find-ById $script:Window "ClearButton" -TimeoutSeconds 5
    if (-not $clear) { return "ClearButton not found in UIA tree" }
    if (-not $clear.Current.IsEnabled) { return "ClearButton.IsEnabled = false" }
    $true
}

# ── 6. Navigate to Chats tab ──────────────────────────────────────────────────
Run-Test "Navigate to Chats tab" {
    $ok = Click-Tab "Chats"
    if (-not $ok) {
        # Try alternative tab name spellings MAUI might use.
        $ok = Click-Tab "Chats" -DelayMs 0
        if (-not $ok) { return "Could not find 'Chats' tab in UIA tree" }
    }
    Start-Sleep -Milliseconds 600
    # Expect the NewChatButton or "Past Conversations" label on the Chats page.
    $newChat = Find-ById $script:Window "NewChatButton" -TimeoutSeconds 6
    $header  = Find-ByName $script:Window "Past Conversations" -TimeoutSeconds 3
    if (-not $newChat -and -not $header) {
        return "After clicking 'Chats' tab: neither NewChatButton nor 'Past Conversations' label found"
    }
    $true
}

# ── 7. Chats page shows list or empty state ───────────────────────────────────
Run-Test "Chats page shows list or empty state" {
    $list  = Find-ById  $script:Window "ConversationsList" -TimeoutSeconds 5
    $empty = Find-ByName $script:Window "No past conversations" -TimeoutSeconds 3
    if (-not $list -and -not $empty) {
        return "Chats page: neither ConversationsList nor empty-state label found"
    }
    $true
}

# ── 8. Navigate to Inbox tab ──────────────────────────────────────────────────
Run-Test "Navigate to Inbox tab" {
    $ok = Click-Tab "Inbox"
    if (-not $ok) { return "Could not find 'Inbox' tab" }
    Start-Sleep -Milliseconds 600
    $inbox = Find-ById $script:Window "InboxList" -TimeoutSeconds 6
    if (-not $inbox) { return "InboxList not found after switching to Inbox tab" }
    $true
}

# ── 9. Inbox page loads without crashing ──────────────────────────────────────
Run-Test "Inbox page loads without crashing" {
    Start-Sleep -Milliseconds 500
    # If the app is still alive and returning elements, it hasn't crashed.
    if ($script:Proc.HasExited) { return "App process exited unexpectedly" }
    $root = $AE::RootElement.FindFirst($Tree::Children,
                (New-Object System.Windows.Automation.PropertyCondition(
                    $AE::ProcessIdProperty, $script:Proc.Id)))
    if (-not $root) { return "App window disappeared from UIA tree - possible crash" }
    $true
}

# ── 10. Navigate back to Chat tab ─────────────────────────────────────────────
Run-Test "Navigate back to Chat tab from Inbox" {
    $ok = Click-Tab "Chat"
    if (-not $ok) { return "Could not find 'Chat' tab" }
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 8
    if (-not $editor) { return "ChatEditor not visible after returning to Chat tab" }
    if ($editor.Current.IsOffscreen) { return "ChatEditor is offscreen after navigating back" }
    $true
}

# ── 11. Rapid tab cycling does not crash ──────────────────────────────────────
Run-Test "Rapid tab cycling does not crash" {
    @("Chats","Inbox","Memory","Logs","Settings","Chat") | ForEach-Object {
        Click-Tab $_ -DelayMs 400 | Out-Null
    }
    Start-Sleep -Milliseconds 600
    if ($script:Proc.HasExited) { return "App exited during rapid tab cycling" }
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 8
    if (-not $editor) { return "ChatEditor not visible after rapid tab cycling" }
    $true
}

# ── 12. Back-navigation from Chats does not crash ────────────────────────────
Run-Test "Back navigation from Chats does not crash" {
    Click-Tab "Chats" -DelayMs 800 | Out-Null
    Start-Sleep -Milliseconds 300
    # In MAUI Shell, tabs are peers — no OS back stack between them.
    # Send Alt+Left to any focusable descendant (window root is not focusable
    # in WinUI 3, so we need a real interactive element to receive the key).
    $focusTarget = Find-ById  $script:Window "NewChatButton" -TimeoutSeconds 3
    if (-not $focusTarget) {
        $focusTarget = Find-ByName $script:Window "New Chat" -TimeoutSeconds 3
    }
    if ($focusTarget) {
        try {
            $focusTarget.SetFocus()
            Start-Sleep -Milliseconds 150
            [System.Windows.Forms.SendKeys]::SendWait("%{LEFT}")
            Start-Sleep -Milliseconds 800
        } catch { }  # SendKeys failure is non-fatal; crash-check below is the real assertion
    }

    if ($script:Proc.HasExited) { return "App process exited after back navigation" }
    # Return to Chat tab to complete the cycle and leave app in clean state.
    Click-Tab "Chat" -DelayMs 600 | Out-Null
    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 6
    if (-not $editor) { return "ChatEditor not visible after back navigation cycle" }
    $true
}

# ── 13. Coco Settings section renders on Windows ─────────────────────────────
Run-Test "Settings page renders Coco section with enable toggle, URL, and Index button" {
    $ok = Click-Tab "Settings" -DelayMs 1000
    if (-not $ok) { return "Could not navigate to Settings tab" }

    $enableLabel = Find-ByName $script:Window "Enable Coco"      -TimeoutSeconds 8
    $urlLabel    = Find-ByName $script:Window "Coco API URL"      -TimeoutSeconds 5
    $indexBtn    = Find-ByName $script:Window "Index Project"     -TimeoutSeconds 5

    if (-not $enableLabel) { return "'Enable Coco' label not found in Settings" }
    if ($enableLabel.Current.IsOffscreen) { return "'Enable Coco' label is offscreen" }
    if (-not $urlLabel)    { return "'Coco API URL' label not found in Settings" }
    if (-not $indexBtn)    { return "'Index Project' button not found in Settings" }
    $true
}

# ── 14. Clipboard monitoring toggle present in Settings Coco section ──────────
Run-Test "Settings Coco section shows clipboard monitoring toggle" {
    # Settings tab already active from previous test, but re-click to be safe.
    Click-Tab "Settings" -DelayMs 600 | Out-Null

    $label = Find-ByName $script:Window "Clipboard monitoring" -TimeoutSeconds 8
    if (-not $label) { return "'Clipboard monitoring' label not found in Settings" }
    if ($label.Current.IsOffscreen) { return "'Clipboard monitoring' label is offscreen" }
    $true
}

# ── 15. Global hotkey field present in Settings Coco section ──────────────────
Run-Test "Settings Coco section shows global hotkey field" {
    Click-Tab "Settings" -DelayMs 600 | Out-Null

    $label = Find-ByName $script:Window "Global hotkey" -TimeoutSeconds 8
    if (-not $label) { return "'Global hotkey' label not found in Settings" }
    if ($label.Current.IsOffscreen) { return "'Global hotkey' label is offscreen" }
    $true
}

# ── 16. Ask Coco toolbar toggle visible in Chat when Coco is enabled ──────────
Run-Test "Ask Coco toolbar toggle is visible in Chat when Coco is enabled" {
    Click-Tab "Settings" -DelayMs 1000 | Out-Null

    $enableLabel = Find-ByName $script:Window "Enable Coco" -TimeoutSeconds 8
    if (-not $enableLabel) { return "'Enable Coco' label not found - cannot enable Coco" }

    # Find the ToggleSwitch that is a sibling of the "Enable Coco" label
    # by walking to the parent container and searching within it.
    $walker     = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $parentEl   = $walker.GetParent($enableLabel)
    $switchCond = New-Object System.Windows.Automation.PropertyCondition(
                      $AE::ClassNameProperty, "ToggleSwitch")

    $cocoSwitch = $null
    if ($parentEl) {
        $cocoSwitch = $parentEl.FindFirst($Tree::Descendants, $switchCond)
    }
    if (-not $cocoSwitch -and $parentEl) {
        $grandparent = $walker.GetParent($parentEl)
        if ($grandparent) {
            $cocoSwitch = $grandparent.FindFirst($Tree::Descendants, $switchCond)
        }
    }

    if ($cocoSwitch) {
        try {
            $togglePat = $cocoSwitch.GetCurrentPattern(
                             [System.Windows.Automation.TogglePattern]::Pattern)
            if ($togglePat.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::Off) {
                $togglePat.Toggle()
                Start-Sleep -Milliseconds 300
            }
        } catch { }
    }

    # Save so IsCocoToggleVisible reads true on the next MainPage creation.
    $saveBtn = Find-ByName $script:Window "Save" -TimeoutSeconds 5
    if ($saveBtn) {
        Invoke-Element $saveBtn | Out-Null
        Start-Sleep -Milliseconds 600
    }

    # Navigate to Chat — a new MainPage evaluates IsCocoToggleVisible from Preferences.
    $ok = Click-Tab "Chat" -DelayMs 800
    if (-not $ok) { return "Could not navigate back to Chat tab" }

    $cocoLabel = Find-ByName $script:Window "🔵 Ask Coco" -TimeoutSeconds 6
    if (-not $cocoLabel) {
        $cocoLabel = Find-ByName $script:Window "Ask Coco" -TimeoutSeconds 3
    }
    if (-not $cocoLabel) { return "Ask Coco toolbar label not found after enabling Coco in Settings" }
    if ($cocoLabel.Current.IsOffscreen) { return "Ask Coco toolbar label is offscreen" }
    $true
}

# ── 17. Chat editor still accessible after Phase 3+4 changes ─────────────────
Run-Test "Chat editor is accessible after Phase 3+4 changes (regression guard)" {
    $ok = Click-Tab "Chat" -DelayMs 800
    if (-not $ok) { return "Could not navigate to Chat tab" }

    $editor = Find-ById $script:Window "ChatEditor" -TimeoutSeconds 8
    if (-not $editor) { return "ChatEditor not found after Phase 3+4 changes" }
    if (-not $editor.Current.IsEnabled) { return "ChatEditor is not enabled" }
    if ($editor.Current.IsOffscreen)    { return "ChatEditor is offscreen" }

    $ok = Set-ElementValue $editor "phase34_guard"
    Start-Sleep -Milliseconds 300
    if (-not $ok) { return "Could not type into ChatEditor" }

    Clear-ElementValue $editor | Out-Null
    $true
}

# ─── Teardown ────────────────────────────────────────────────────────────────

if (-not $KeepAppOpen -and $script:Proc -and -not $script:Proc.HasExited) {
    $script:Proc.CloseMainWindow() | Out-Null
    $script:Proc.WaitForExit(3000) | Out-Null
    if (-not $script:Proc.HasExited) { $script:Proc.Kill() }
}

# ─── Summary ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "----------------------------------------------" -ForegroundColor DarkGray
$colour = if ($script:Failed -eq 0) { "Green" } else { "Yellow" }
Write-Host "Results: $($script:Passed) passed  /  $($script:Failed) failed" -ForegroundColor $colour
Write-Host "----------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

if ($script:Failed -gt 0) { exit 1 } else { exit 0 }
