@echo off
echo ========================================
echo    LeetCode Compiler Backend Startup
echo ========================================
echo.

echo [DEBUG] Current directory: %CD%
echo [DEBUG] Checking if we're in the right folder...
if not exist "LeetCodeCompiler.API.csproj" (
    echo ERROR: LeetCodeCompiler.API.csproj not found!
    echo Please run this script from the LeetCodeCompiler.API folder.
    echo Current directory: %CD%
    pause
    exit /b 1
)

echo [DEBUG] Found project file. Checking .NET...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found in PATH!
    echo Please install .NET 8 SDK or open a Developer Command Prompt.
    echo Download: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [DEBUG] .NET found. Checking Docker...
docker info >nul 2>&1
if %errorlevel% equ 0 (
    echo [DEBUG] Docker is already running. Skipping startup.
) else (
    echo [DEBUG] Docker is not running. Starting Docker Desktop...
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    echo [DEBUG] Waiting 30 seconds for Docker...
    timeout /t 30 /nobreak >nul
)

echo [DEBUG] Starting .NET Backend API...
echo Backend will be available at: http://0.0.0.0:5081
echo Swagger UI will be available at: http://0.0.0.0:5081/swagger/index.html
echo.
echo Press Ctrl+C to stop the backend
echo ========================================
echo.

echo [DEBUG] Running: dotnet run
dotnet run

echo.
echo [DEBUG] Backend has stopped with exit code: %errorlevel%
echo Press any key to close this window...
pause >nul
