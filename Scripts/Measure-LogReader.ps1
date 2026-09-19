param(
    [int]$EntryCount = 100000,
    [int]$PageSize = 200,
    [int]$SampleCount = 40
)

$ErrorActionPreference = 'Stop'
$assemblyPath = Resolve-Path "$PSScriptRoot\..\LaaUnitTests\bin\Debug\net9.0\LaaUnitTests.dll"
Add-Type -Path $assemblyPath
$testPath = [IO.Path]::GetTempFileName()

try {
    $utf8 = [Text.UTF8Encoding]::new($false)
    $writer = [IO.StreamWriter]::new($testPath, $false, $utf8, 65536)
    try {
        for ($index = 0; $index -lt $EntryCount; $index++) {
            $timestamp = [DateTime]::UtcNow.AddMilliseconds($index).ToString('O')
            $writer.WriteLine('{"@t":"' + $timestamp + '","@mt":"Synthetic representative log entry ' + $index + '","@l":"Information","Category":"Performance"}')
        }
    }
    finally {
        $writer.Dispose()
    }

    [LocalAIAssistant.Services.Logging.NewestLogLineReader]::ReadRecords(
        $testPath,
        $PageSize,
        [Threading.CancellationToken]::None) | Out-Null

    $samples = foreach ($run in 1..$SampleCount) {
        $timer = [Diagnostics.Stopwatch]::StartNew()
        [LocalAIAssistant.Services.Logging.NewestLogLineReader]::ReadRecords(
            $testPath,
            $PageSize,
            [Threading.CancellationToken]::None) | Out-Null
        $timer.Stop()
        $timer.Elapsed.TotalMilliseconds
    }

    $sorted = @($samples | Sort-Object)
    $p50 = $sorted[[Math]::Floor(($sorted.Count - 1) * 0.50)]
    $p95 = $sorted[[Math]::Floor(($sorted.Count - 1) * 0.95)]

    [pscustomobject]@{
        Entries = $EntryCount
        FileMiB = [Math]::Round((Get-Item $testPath).Length / 1MB, 2)
        PageSize = $PageSize
        Samples = $SampleCount
        P50Milliseconds = [Math]::Round($p50, 2)
        P95Milliseconds = [Math]::Round($p95, 2)
        MaximumMilliseconds = [Math]::Round(($sorted | Select-Object -Last 1), 2)
    }
}
finally {
    Remove-Item -LiteralPath $testPath -Force -ErrorAction SilentlyContinue
}
