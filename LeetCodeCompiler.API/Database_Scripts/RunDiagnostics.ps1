# =============================================
# PowerShell Script to Run Database Diagnostics
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Server=localhost;Database=LeetCodeCompiler;Integrated Security=true;TrustServerCertificate=true;"
)

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Running Database Diagnostics" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

try {
    # Read the diagnostic SQL script
    $sqlScript = Get-Content -Path "CheckSubmissionConstraints.sql" -Raw
    
    if (-not $sqlScript) {
        throw "Could not read the diagnostic SQL script file"
    }

    Write-Host "Executing diagnostic SQL script..." -ForegroundColor Cyan
    
    # Execute the SQL script
    $results = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $sqlScript
    
    Write-Host "✅ Diagnostics completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Results:" -ForegroundColor Yellow
    Write-Host "--------" -ForegroundColor Yellow
    
    # Display results in a more readable format
    $results | Format-Table -AutoSize
    
} catch {
    Write-Host "❌ Error executing diagnostic script: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Check your connection string" -ForegroundColor White
    Write-Host "2. Ensure SQL Server is running" -ForegroundColor White
    Write-Host "3. Verify database permissions" -ForegroundColor White
    
    exit 1
}

Write-Host ""
Write-Host "Diagnostic script completed!" -ForegroundColor Green
