# PowerShell script to check database status and provide specific guidance
Write-Host "=============================================" -ForegroundColor Green
Write-Host "CODING TEST API DATABASE STATUS CHECK" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5081"

# Test 1: Check if API is running
Write-Host "1. Checking if API is running..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method GET -ContentType "application/json" -TimeoutSec 5
    Write-Host "   ✓ API is running and accessible" -ForegroundColor Green
} catch {
    Write-Host "   ✗ API Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure the application is running on $baseUrl" -ForegroundColor Yellow
    Write-Host "   Run: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test 2: Try to create a minimal test to see the exact error
Write-Host ""
Write-Host "2. Testing minimal coding test creation..." -ForegroundColor Cyan
$minimalTest = @{
    testName = "Database Test"
    createdBy = 1
    startDate = "2025-01-20T10:00:00Z"
    endDate = "2025-01-20T12:00:00Z"
    durationMinutes = 60
    totalQuestions = 1
    totalMarks = 10
    testType = "Practice"
    allowMultipleAttempts = $false
    maxAttempts = 1
    showResultsImmediately = $true
    allowCodeReview = $false
    accessCode = "DBTEST123"
    tags = "database-test"
    isResultPublishAutomatically = $true
    applyBreachRule = $true
    breachRuleLimit = 0
    hostIP = "127.0.0.1"
    classId = 1
    topicData = @(
        @{
            sectionId = 1
            domainId = 1
            subdomainId = 1
        }
    )
    questions = @(
        @{
            problemId = 1
            questionOrder = 1
            marks = 10
            timeLimitMinutes = 30
            customInstructions = "Database test question"
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method POST -Body $minimalTest -ContentType "application/json"
    Write-Host "   ✓ SUCCESS: Test created! Database is properly set up." -ForegroundColor Green
    Write-Host "   Test ID: $($response.id)" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 Your database is working correctly!" -ForegroundColor Green
    Write-Host "You can now use your original request." -ForegroundColor Green
} catch {
    Write-Host "   ✗ FAILED: $($_.Exception.Message)" -ForegroundColor Red
    
    # Get detailed error information
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        
        Write-Host ""
        Write-Host "Detailed Error Response:" -ForegroundColor Red
        Write-Host $errorBody -ForegroundColor White
        
        # Check for specific error patterns
        if ($errorBody -match "entity changes" -or $errorBody -match "table.*doesn.*exist" -or $errorBody -match "foreign key") {
            Write-Host ""
            Write-Host "🔍 DIAGNOSIS: Database tables are missing!" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "SOLUTION STEPS:" -ForegroundColor Yellow
            Write-Host "1. Open SQL Server Management Studio" -ForegroundColor White
            Write-Host "2. Connect to your LeetCode database" -ForegroundColor White
            Write-Host "3. Run the script: Database_Scripts/SimpleFix.sql" -ForegroundColor White
            Write-Host "4. Test your API again" -ForegroundColor White
            Write-Host ""
            Write-Host "The script will create:" -ForegroundColor Cyan
            Write-Host "- CodingTests table" -ForegroundColor White
            Write-Host "- CodingTestQuestions table" -ForegroundColor White
            Write-Host "- CodingTestTopicData table" -ForegroundColor White
            Write-Host "- Sample problems (IDs 1-5)" -ForegroundColor White
        }
    }
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "STATUS CHECK COMPLETE" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
