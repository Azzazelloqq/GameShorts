# ShortGames Core Test Runner - Windows PowerShell Script
# Usage: .\RunTests.ps1 [-TestFilter "filter"] [-UnityPath "path"]

param(
    [string]$TestFilter = "Code.Core.ShotGamesCore.Tests",
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2021.3.16f1\Editor\Unity.exe"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ShortGames Core Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if Unity exists at the specified path
if (-not (Test-Path $UnityPath)) {
    Write-Host "Error: Unity not found at $UnityPath" -ForegroundColor Red
    Write-Host "Please specify the correct Unity path using -UnityPath parameter" -ForegroundColor Yellow
    exit 1
}

$ProjectPath = Get-Location
Write-Host "Project Path: $ProjectPath" -ForegroundColor Green
Write-Host "Unity Path: $UnityPath" -ForegroundColor Green
Write-Host "Test Filter: $TestFilter" -ForegroundColor Green
Write-Host ""

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
$arguments = @(
    "-batchmode",
    "-nographics", 
    "-silent-crashes",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testFilter", $TestFilter,
    "-testResults", "TestResults.xml",
    "-logFile", "TestLog.txt"
)

& $UnityPath $arguments

# Check if test results file was created
if (Test-Path "TestResults.xml") {
    Write-Host "Tests completed. Results saved to TestResults.xml" -ForegroundColor Green
    
    # Parse and display summary
    [xml]$results = Get-Content "TestResults.xml"
    $summary = $results."test-run"
    
    Write-Host ""
    Write-Host "Test Summary:" -ForegroundColor Cyan
    Write-Host "  Total: $($summary.total)" -ForegroundColor White
    Write-Host "  Passed: $($summary.passed)" -ForegroundColor Green
    Write-Host "  Failed: $($summary.failed)" -ForegroundColor Red
    Write-Host "  Skipped: $($summary.skipped)" -ForegroundColor Yellow
    Write-Host "  Duration: $($summary.duration) seconds" -ForegroundColor White
    
    # Show failed tests if any
    if ([int]$summary.failed -gt 0) {
        Write-Host ""
        Write-Host "Failed Tests:" -ForegroundColor Red
        $failedTests = $results.SelectNodes("//test-case[@result='Failed']")
        foreach ($test in $failedTests) {
            Write-Host "  - $($test.name)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "Warning: Test results file not found. Check TestLog.txt for details." -ForegroundColor Yellow
}

# Display log file location
Write-Host ""
Write-Host "Log file: TestLog.txt" -ForegroundColor Gray

# Exit with appropriate code
if (Test-Path "TestResults.xml") {
    [xml]$results = Get-Content "TestResults.xml"
    if ([int]$results."test-run".failed -gt 0) {
        exit 1
    }
}
exit 0