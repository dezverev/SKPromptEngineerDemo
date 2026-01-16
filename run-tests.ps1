# Run IntegrationTesterApp tests
# Usage: .\run-tests.ps1 [--verbose|-v]

param(
    [switch]$Verbose
)

Write-Host "Running IntegrationTesterApp tests..." -ForegroundColor Cyan

$originalLocation = Get-Location
try {
    Set-Location "$PSScriptRoot\src\IntegrationTesterApp"
    
    if ($Verbose) {
        dotnet run -- --verbose
    } else {
        dotnet run
    }
    
    $exitCode = $LASTEXITCODE
} finally {
    Set-Location $originalLocation
}

exit $exitCode
