@echo off
echo Applying Overdue Field Migration...
echo.
echo This will add the Overdue column to Actions, CorrectiveActions, and SubActions tables.
echo.
pause

REM Run the SQL script using sqlcmd
sqlcmd -S (localdb)\MSSQLLocalDB -d HSEDatabase -i "Manual_AddOverdueField.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Migration applied successfully!
) else (
    echo.
    echo Migration failed with error level %ERRORLEVEL%
)

pause