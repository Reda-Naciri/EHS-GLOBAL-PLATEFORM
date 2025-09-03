@echo off
echo Applying Applications table migration...
cd /d "%~dp0.."

echo Stopping any running backend processes...
taskkill /f /im "HSEBackend.exe" 2>nul

echo Building the project...
dotnet build
if %errorlevel% neq 0 (
    echo Build failed. Please fix the errors and try again.
    pause
    exit /b 1
)

echo Applying database migration...
dotnet ef database update
if %errorlevel% neq 0 (
    echo Migration failed. Please check the error above.
    pause
    exit /b 1
)

echo Applications table created successfully!
echo You can now start the backend again.
pause