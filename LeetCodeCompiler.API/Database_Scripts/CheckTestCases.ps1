# =============================================
# PowerShell Script to Check TestCase IDs
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Server=localhost;Database=LeetCodeCompiler;Integrated Security=true;TrustServerCertificate=true;"
)

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Checking TestCase IDs" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

try {
    # Read the SQL script
    $sqlScript = Get-Content -Path "CheckTestCases.sql" -Raw
    
    if (-not $sqlScript) {
        throw "Could not read the CheckTestCases.sql script file"
    }

    Write-Host "Executing TestCase check script..." -ForegroundColor Cyan
    
    # Execute the SQL script
    $results = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $sqlScript
    
    Write-Host "✅ TestCase check completed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Results:" -ForegroundColor Yellow
    Write-Host "--------" -ForegroundColor Yellow
    
    # Display results in a more readable format
    $results | Format-Table -AutoSize
    
} catch {
    Write-Host "❌ Error executing TestCase check: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "TestCase check completed!" -ForegroundColor Green
