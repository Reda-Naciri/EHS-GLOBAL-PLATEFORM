# PowerShell script to run the report status migration
# This script connects to the database and executes the UpdateReportStatuses.sql script

param(
    [string]$ConnectionString = "Data Source=HSE_Database.db",
    [string]$SqlFile = "UpdateReportStatuses.sql"
)

Write-Host "Report Status Migration Script" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""

# Check if SQL file exists
$sqlFilePath = Join-Path $PSScriptRoot $SqlFile
if (-not (Test-Path $sqlFilePath)) {
    Write-Error "SQL file not found: $sqlFilePath"
    exit 1
}

Write-Host "SQL File: $sqlFilePath" -ForegroundColor Yellow
Write-Host "Connection: $ConnectionString" -ForegroundColor Yellow
Write-Host ""

# For SQLite, we'll use the sqlite3 command line tool
# First check if sqlite3 is available
try {
    $sqliteVersion = sqlite3 -version
    Write-Host "SQLite Version: $sqliteVersion" -ForegroundColor Cyan
} catch {
    Write-Error "sqlite3 command not found. Please install SQLite or ensure it's in your PATH."
    Write-Host "For Windows, you can download SQLite from: https://www.sqlite.org/download.html"
    exit 1
}

Write-Host ""
Write-Host "Executing migration script..." -ForegroundColor Yellow

# Navigate to the HSEBackend directory to find the database
$backendPath = Split-Path $PSScriptRoot -Parent
$dbPath = Join-Path $backendPath "HSE_Database.db"

if (-not (Test-Path $dbPath)) {
    Write-Error "Database file not found: $dbPath"
    Write-Host "Please ensure the backend has been run at least once to create the database."
    exit 1
}

try {
    # Execute the SQL script
    $result = sqlite3 $dbPath ".read $sqlFilePath"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migration completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Verifying current status counts..." -ForegroundColor Yellow
        
        # Query current status counts
        $statusQuery = @"
SELECT 
    Status,
    COUNT(*) as Count
FROM Reports 
GROUP BY Status 
ORDER BY Status;
"@
        
        Write-Host "Current report status counts:" -ForegroundColor Cyan
        sqlite3 $dbPath $statusQuery
        
    } else {
        Write-Error "Migration failed with exit code: $LASTEXITCODE"
        Write-Host "Output: $result"
    }
} catch {
    Write-Error "Failed to execute migration: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "Migration script completed." -ForegroundColor Green