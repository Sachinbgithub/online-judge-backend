# =============================================
# PowerShell Script to Recreate CodingTestSubmissions Tables
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Server=localhost;Database=LeetCodeCompiler;Integrated Security=true;TrustServerCertificate=true;",
    
    [Parameter(Mandatory=$false)]
    [switch]$Force = $false
)

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Recreating CodingTestSubmissions Tables" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Check if Force flag is provided
if (-not $Force) {
    $confirmation = Read-Host "This will DROP and RECREATE the CodingTestSubmissions tables. Are you sure? (y/N)"
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit
    }
}

try {
    # Read the SQL script
    $sqlScript = Get-Content -Path "RecreateCodingTestSubmissionsTables.sql" -Raw
    
    if (-not $sqlScript) {
        throw "Could not read the SQL script file"
    }

    Write-Host "Executing SQL script..." -ForegroundColor Cyan
    
    # Execute the SQL script
    Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $sqlScript
    
    Write-Host "✅ Tables recreated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Restart your API application" -ForegroundColor White
    Write-Host "2. Test the submit-whole-test endpoint" -ForegroundColor White
    Write-Host ""
    Write-Host "Tables created:" -ForegroundColor Cyan
    Write-Host "- CodingTestSubmissions" -ForegroundColor White
    Write-Host "- CodingTestSubmissionResults" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error executing SQL script: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Check your connection string" -ForegroundColor White
    Write-Host "2. Ensure SQL Server is running" -ForegroundColor White
    Write-Host "3. Verify database permissions" -ForegroundColor White
    Write-Host "4. Check if tables are being used by other processes" -ForegroundColor White
    
    exit 1
}

Write-Host ""
Write-Host "Script completed successfully!" -ForegroundColor Green
