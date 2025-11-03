# LeetCode Compiler Backend Startup Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   LeetCode Compiler Backend Startup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/4] Checking Docker status..." -ForegroundColor Yellow
$dockerRunning = $false
try {
    docker info 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $dockerRunning = $true }
} catch { $dockerRunning = $false }

if ($dockerRunning) {
    Write-Host "Docker is already running. Skipping startup." -ForegroundColor Green
} else {
    Write-Host "Docker is not running. Starting Docker Desktop..." -ForegroundColor Yellow
    try {
        Start-Process -FilePath "C:\Program Files\Docker\Docker\Docker Desktop.exe" -WindowStyle Hidden
        Write-Host "Docker Desktop is starting..." -ForegroundColor Green
    } catch {
        Write-Host "Could not start Docker Desktop. Please start it manually." -ForegroundColor Red
        Write-Host "Continuing anyway..." -ForegroundColor Yellow
    }
}

Write-Host ""
if (-not $dockerRunning) {
    Write-Host "[2/4] Waiting for Docker to be ready (30 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30
} else {
    Write-Host "[2/4] Docker already running. Skipping wait." -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/4] Verifying Docker is ready..." -ForegroundColor Yellow
try {
    $dockerVersion = docker version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Docker is ready!" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Docker might not be fully ready yet, but continuing..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "WARNING: Could not verify Docker status, but continuing..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[4/4] Starting .NET Backend API..." -ForegroundColor Yellow
Write-Host "Backend will be available at: http://0.0.0.0:5081" -ForegroundColor Cyan
Write-Host "Swagger UI will be available at: http://0.0.0.0:5081/swagger/index.html" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: Use your actual IP address to access from other devices" -ForegroundColor Yellow
Write-Host "Example: http://192.168.1.100:5081 (replace with your IP)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the backend" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Change to script directory and run the backend
Set-Location $PSScriptRoot
try {
    dotnet run
} catch {
    Write-Host "Error starting the backend: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Backend has stopped." -ForegroundColor Yellow
Read-Host "Press Enter to exit"
