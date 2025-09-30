# PowerShell script to test the specific request that's failing
Write-Host "Testing the specific Coding Test creation request..." -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5081"

# The exact request from the user
$testRequest = @{
    testName = "elon"
    createdBy = 4021
    startDate = "2025-09-25T14:29:12.266Z"
    endDate = "2025-09-25T14:29:12.266Z"
    durationMinutes = 40
    totalQuestions = 1
    totalMarks = 10
    testType = "coding"
    allowMultipleAttempts = $true
    maxAttempts = 10
    showResultsImmediately = $true
    allowCodeReview = $true
    accessCode = "string"
    tags = "string"
    isResultPublishAutomatically = $true
    applyBreachRule = $true
    breachRuleLimit = 0
    hostIP = "string"
    classId = 0
    topicData = @(
        @{
            sectionId = 8049
            domainId = 1
            subdomainId = 2
        }
    )
    questions = @(
        @{
            problemId = 1
            questionOrder = 100
            marks = 100
            timeLimitMinutes = 120
            customInstructions = "string"
        }
    )
} | ConvertTo-Json -Depth 10

Write-Host "Request JSON:" -ForegroundColor Cyan
Write-Host $testRequest -ForegroundColor White
Write-Host ""

# Test the request
Write-Host "Sending request to API..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method POST -Body $testRequest -ContentType "application/json"
    Write-Host "✓ SUCCESS: Test created!" -ForegroundColor Green
    Write-Host "Test ID: $($response.id)" -ForegroundColor Green
} catch {
    Write-Host "✗ FAILED: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try to get detailed error information
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host ""
        Write-Host "Detailed Error Response:" -ForegroundColor Red
        Write-Host $errorBody -ForegroundColor White
        
        # Parse JSON error if possible
        try {
            $errorJson = $errorBody | ConvertFrom-Json
            if ($errorJson.details) {
                Write-Host ""
                Write-Host "Error Details: $($errorJson.details)" -ForegroundColor Red
            }
        } catch {
            Write-Host "Could not parse error as JSON" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Yellow
Write-Host "COMMON SOLUTIONS:" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Yellow
Write-Host "1. Run DiagnoseAndFix.sql in SQL Server Management Studio" -ForegroundColor White
Write-Host "2. Run CreateCodingTestTables.sql to create missing tables" -ForegroundColor White
Write-Host "3. Ensure Problems table has data (problemId: 1 must exist)" -ForegroundColor White
Write-Host "4. Check database connection string in appsettings.json" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Yellow
