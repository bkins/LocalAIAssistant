<#
.SYNOPSIS
    LAA (Local AI Assistant) Unified Regression Test Runner
.DESCRIPTION
    Orchestrates Windows and Android UI regression suites.
    Manages state setups, gathers test statistics, captures failure artifacts,
    and formats final reports.
.PARAMETER Platform
    Specify which platform to test: Windows, Android, or All. Default: Windows.
.PARAMETER KeepAppOpen
    Leave the app running after a test run.
.EXAMPLE
    .\Run-RegressionTests.ps1 -Platform Windows
#>
param(
    [ValidateSet("Windows", "Android", "All")]
    [string] $Platform = "Windows",
    [switch] $KeepAppOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Import common helpers
$TestDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$TestDir\Test-Helpers.ps1"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  LAA Unified Regression Test Runner      " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Platform Target: $Platform"
Write-Host "Current Time   : $(Get-Date)"
Write-Host "==========================================" -ForegroundColor Cyan

$TotalPassed = 0
$TotalFailed = 0
$SuitesRun = @()

# ─── Windows Execution Hook ──────────────────────────────────────────────────
if ($Platform -eq "Windows" -or $Platform -eq "All") {
    Write-Host "`n[Suite] Running Windows Regression Tests..." -ForegroundColor Yellow
    $winScript = Join-Path $TestDir "LAA.SmokeTests.Windows\SmokeTest-Windows.ps1"
    
    if (Test-Path $winScript) {
        # Initialize/clean DB before running
        Clean-SqliteDatabase
        
        $exitCode = 0
        try {
            $keepOpenFlag = if ($KeepAppOpen) { "-KeepAppOpen" } else { "" }
            # Execute the script
            # Capture output and pass-through exit code
            & $winScript $keepOpenFlag
            $exitCode = $LASTEXITCODE
        } catch {
            Write-Host "Execution Error: $_" -ForegroundColor Red
            $exitCode = 1
        }
        
        if ($exitCode -eq 0) {
            $TotalPassed++
            $SuitesRun += [PSCustomObject]@{ Suite = "Windows"; Status = "PASS" }
        } else {
            $TotalFailed++
            $SuitesRun += [PSCustomObject]@{ Suite = "Windows"; Status = "FAIL" }
            # Capture failure screenshot
            Take-WindowsScreenshot -FileNamePrefix "LAA_Win_Regression_Fail" | Out-Null
        }
    } else {
        Write-Host "Windows test script not found: $winScript" -ForegroundColor Red
        $TotalFailed++
    }
}

# ─── Android Execution Hook ──────────────────────────────────────────────────
if ($Platform -eq "Android" -or $Platform -eq "All") {
    Write-Host "`n[Suite] Running Android Regression Tests..." -ForegroundColor Yellow
    $androidScript = Join-Path $TestDir "LAA.SmokeTests.Android\SmokeTest-Android.ps1"
    
    if (Test-Path $androidScript) {
        $exitCode = 0
        try {
            $keepOpenFlag = if ($KeepAppOpen) { "-KeepAppOpen" } else { "" }
            & $androidScript $keepOpenFlag
            $exitCode = $LASTEXITCODE
        } catch {
            Write-Host "Execution Error: $_" -ForegroundColor Red
            $exitCode = 1
        }
        
        if ($exitCode -eq 0) {
            $TotalPassed++
            $SuitesRun += [PSCustomObject]@{ Suite = "Android"; Status = "PASS" }
        } else {
            $TotalFailed++
            $SuitesRun += [PSCustomObject]@{ Suite = "Android"; Status = "FAIL" }
            # Capture failure screenshot
            Take-AndroidScreenshot -Device $null -FileNamePrefix "LAA_Android_Regression_Fail" | Out-Null
        }
    } else {
        Write-Host "Android test script not found: $androidScript" -ForegroundColor Red
        $TotalFailed++
    }
}

# ─── Report Generator ────────────────────────────────────────────────────────
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "  Regression Run Summary                  " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

foreach ($suite in $SuitesRun) {
    $color = if ($suite.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "[$($suite.Status)] $($suite.Suite) Test Suite" -ForegroundColor $color
}

Write-Host "------------------------------------------"
$finalColor = if ($TotalFailed -eq 0) { "Green" } else { "Red" }
Write-Host "Suites Passed: $TotalPassed | Suites Failed: $TotalFailed" -ForegroundColor $finalColor
Write-Host "==========================================" -ForegroundColor Cyan

if ($TotalFailed -gt 0) {
    exit 1
} else {
    exit 0
}
