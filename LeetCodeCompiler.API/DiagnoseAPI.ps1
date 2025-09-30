# PowerShell script to diagnose the Coding Test API issue
Write-Host "Diagnosing Coding Test API Issue..." -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5081"

# Test 1: Check if API is running
Write-Host "1. Testing API connectivity..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method GET -ContentType "application/json"
    Write-Host "   ✓ API is running and accessible" -ForegroundColor Green
} catch {
    Write-Host "   ✗ API Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure the application is running on $baseUrl" -ForegroundColor Yellow
    exit 1
}

# Test 2: Try to create a minimal test
Write-Host "2. Testing minimal coding test creation..." -ForegroundColor Cyan
$minimalTest = @{
    testName = "Diagnostic Test"
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
    accessCode = "DIAG123"
    tags = "diagnostic"
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
            customInstructions = "Diagnostic question"
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method POST -Body $minimalTest -ContentType "application/json"
    Write-Host "   ✓ Minimal test created successfully" -ForegroundColor Green
    Write-Host "   Test ID: $($response.id)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Creation failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try to get more details from the error
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error details: $errorBody" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Diagnosis Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Common Issues and Solutions:" -ForegroundColor Yellow
Write-Host "1. Database tables don't exist - Run QuickCreateTables.sql" -ForegroundColor White
Write-Host "2. Problem with ID 1 doesn't exist - Check Problems table" -ForegroundColor White
Write-Host "3. Database connection issues - Check connection string" -ForegroundColor White
Write-Host "4. Foreign key constraints - Ensure referenced records exist" -ForegroundColor White
