# PowerShell script to verify Coding Test API endpoints
# Run this script to test if the API endpoints are accessible

Write-Host "Testing Coding Test API Endpoints..." -ForegroundColor Green
Write-Host "Make sure the application is running on http://localhost:5081" -ForegroundColor Yellow
Write-Host ""

$baseUrl = "http://localhost:5081"

# Test 1: Get all coding tests
Write-Host "1. Testing GET /api/CodingTest..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest" -Method GET -ContentType "application/json"
    Write-Host "   ✓ Success: Found $($response.Count) coding tests" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get Swagger documentation
Write-Host "2. Testing Swagger documentation..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method GET
    Write-Host "   ✓ Success: Swagger documentation accessible" -ForegroundColor Green
    
    # Check if CodingTest endpoints are in the swagger
    $swaggerContent = $response | ConvertTo-Json -Depth 10
    if ($swaggerContent -match "CodingTest") {
        Write-Host "   ✓ Success: CodingTest endpoints found in Swagger" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ Warning: CodingTest endpoints not found in Swagger" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Test a specific endpoint
Write-Host "3. Testing GET /api/CodingTest/status/active..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CodingTest/status/active" -Method GET -ContentType "application/json"
    Write-Host "   ✓ Success: Status endpoint working" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "API Testing Complete!" -ForegroundColor Green
Write-Host "If all tests passed, your Coding Test API is working correctly." -ForegroundColor Green
Write-Host "You can now access Swagger UI at: http://localhost:5081/swagger" -ForegroundColor Yellow
