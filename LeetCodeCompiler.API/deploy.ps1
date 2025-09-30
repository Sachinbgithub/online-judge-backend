# LeetCode Compiler API Deployment Script
# This script helps deploy the API for network access

Write-Host "Building LeetCode Compiler API..." -ForegroundColor Green
dotnet build --configuration Release

Write-Host "Publishing LeetCode Compiler API..." -ForegroundColor Green
dotnet publish --configuration Release --output ./publish

Write-Host "Starting LeetCode Compiler API..." -ForegroundColor Green
Write-Host "The API will be available at:" -ForegroundColor Yellow
Write-Host "  HTTP:  http://[YOUR_IP_ADDRESS]:5081" -ForegroundColor Cyan
Write-Host "  HTTPS: https://[YOUR_IP_ADDRESS]:7169" -ForegroundColor Cyan
Write-Host "  Swagger: http://[YOUR_IP_ADDRESS]:5081/swagger" -ForegroundColor Cyan

Write-Host "`nTo find your IP address, run: ipconfig" -ForegroundColor Yellow
Write-Host "Make sure your firewall allows connections on ports 5081 and 7169" -ForegroundColor Yellow

# Start the application
dotnet run --configuration Release --urls "http://0.0.0.0:5081;https://0.0.0.0:7169" 