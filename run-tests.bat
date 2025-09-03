@echo off
title HSEBackend API Testing Suite

echo ========================================
echo HSEBackend API Testing Suite
echo ========================================
echo.

:: Check if PowerShell is available
powershell -Command "Write-Host 'PowerShell is available'" >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: PowerShell is not available on this system
    pause
    exit /b 1
)

:: Set default values
set BASE_URL=http://localhost:5225
set JWT_TOKEN=
set PUBLIC_ONLY=false
set VERBOSE=false

:: Parse command line arguments
:parse_args
if "%~1"=="" goto :run_tests
if "%~1"=="-url" (
    set BASE_URL=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="-token" (
    set JWT_TOKEN=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="-public" (
    set PUBLIC_ONLY=true
    shift
    goto :parse_args
)
if "%~1"=="-verbose" (
    set VERBOSE=true
    shift
    goto :parse_args
)
if "%~1"=="-help" (
    goto :show_help
)
shift
goto :parse_args

:run_tests
echo Configuration:
echo - Base URL: %BASE_URL%
echo - JWT Token: %JWT_TOKEN%
echo - Public Only: %PUBLIC_ONLY%
echo - Verbose: %VERBOSE%
echo.

echo Starting API tests...
echo.

:: Run the PowerShell script
if "%JWT_TOKEN%"=="" (
    if "%PUBLIC_ONLY%"=="true" (
        powershell -ExecutionPolicy Bypass -File "%~dp0Test-HSEBackend.ps1" -BaseUrl "%BASE_URL%" -PublicOnly -Verbose:%VERBOSE%
    ) else (
        powershell -ExecutionPolicy Bypass -File "%~dp0Test-HSEBackend.ps1" -BaseUrl "%BASE_URL%" -Verbose:%VERBOSE%
    )
) else (
    if "%PUBLIC_ONLY%"=="true" (
        powershell -ExecutionPolicy Bypass -File "%~dp0Test-HSEBackend.ps1" -BaseUrl "%BASE_URL%" -JwtToken "%JWT_TOKEN%" -PublicOnly -Verbose:%VERBOSE%
    ) else (
        powershell -ExecutionPolicy Bypass -File "%~dp0Test-HSEBackend.ps1" -BaseUrl "%BASE_URL%" -JwtToken "%JWT_TOKEN%" -Verbose:%VERBOSE%
    )
)

echo.
echo Testing completed.
pause
exit /b %errorlevel%

:show_help
echo Usage: run-tests.bat [options]
echo.
echo Options:
echo   -url ^<base_url^>     Set the base URL (default: http://localhost:5225)
echo   -token ^<jwt_token^>  Set the JWT token for authenticated tests
echo   -public             Run only public endpoint tests
echo   -verbose            Enable verbose output
echo   -help               Show this help message
echo.
echo Examples:
echo   run-tests.bat
echo   run-tests.bat -url http://localhost:8080
echo   run-tests.bat -token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
echo   run-tests.bat -public -verbose
echo   run-tests.bat -url http://localhost:8080 -token "your_jwt_token" -verbose
echo.
pause
exit /b 0