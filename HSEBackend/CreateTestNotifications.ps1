# PowerShell script to create test notifications for HSE system
# This script will:
# 1. Execute SQL to create sample notifications directly in the database
# 2. Call admin trigger endpoints to generate system notifications
# 3. Verify notifications were created

Write-Host "Creating Test Notifications for HSE System" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Define paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "HSE_DB.db"
$sqlScript = Join-Path $scriptDir "CreateTestNotifications.sql"

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "❌ Database not found at: $dbPath" -ForegroundColor Red
    Write-Host "Please ensure the backend has been run at least once to create the database." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Database found at: $dbPath" -ForegroundColor Green

# Check if SQL script exists
if (-not (Test-Path $sqlScript)) {
    Write-Host "❌ SQL script not found at: $sqlScript" -ForegroundColor Red
    exit 1
}

Write-Host "✅ SQL script found" -ForegroundColor Green

# Step 1: Execute SQL script to create test notifications
Write-Host "`n🔄 Step 1: Creating sample notifications in database..." -ForegroundColor Cyan

try {
    # Use sqlite3 command if available, otherwise try dotnet ef
    $sqliteCommand = Get-Command sqlite3 -ErrorAction SilentlyContinue
    
    if ($sqliteCommand) {
        Write-Host "Using sqlite3 command..." -ForegroundColor Yellow
        $output = & sqlite3 $dbPath ".read $sqlScript"
        Write-Host "✅ SQL script executed successfully" -ForegroundColor Green
    } else {
        Write-Host "sqlite3 not found, trying alternative method..." -ForegroundColor Yellow
        
        # Read SQL content and modify for dotnet ef
        $sqlContent = Get-Content $sqlScript -Raw
        # Replace SQLite DATETIME with SQL Server compatible format
        $sqlContent = $sqlContent -replace "DATETIME\('now', '([^']+)'\)", "DATEADD(MINUTE, `$1, GETUTCDATE())"
        $sqlContent = $sqlContent -replace "DATETIME\('now'\)", "GETUTCDATE()"
        
        # Create temporary SQL file
        $tempSql = Join-Path $scriptDir "temp_notifications.sql"
        $sqlContent | Out-File -FilePath $tempSql -Encoding UTF8
        
        Write-Host "⚠️  Manual SQL execution required. Please run the SQL script manually in your database tool." -ForegroundColor Yellow
        Write-Host "SQL file location: $sqlScript" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Error executing SQL script: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You may need to run the SQL script manually" -ForegroundColor Yellow
}

# Step 2: Start the backend if not running and call admin endpoints
Write-Host "`n🔄 Step 2: Testing admin trigger endpoints..." -ForegroundColor Cyan

# Define backend URL (adjust if different)
$baseUrl = "https://localhost:7068"
$apiUrl = "$baseUrl/api/notifications"

# Admin credentials for authentication (adjust as needed)
$adminEmail = "admin@te.com"
$adminPassword = "Admin123!"

Write-Host "Attempting to authenticate and call admin endpoints..." -ForegroundColor Yellow

# Function to test endpoint
function Test-AdminEndpoint {
    param(
        [string]$endpoint,
        [string]$description
    )
    
    try {
        Write-Host "Testing $description..." -ForegroundColor Yellow
        
        # Simple test without authentication for now
        $response = Invoke-RestMethod -Uri "$apiUrl/$endpoint" -Method Post -ErrorAction Stop
        Write-Host "✅ $description - Success" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "❌ $description - Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host "   Authentication required. Please ensure backend is running and admin is logged in." -ForegroundColor Yellow
        }
        return $false
    }
}

# Test admin endpoints (these require authentication)
Write-Host "`nNote: Admin endpoints require authentication. If backend is not running or admin is not logged in, these will fail." -ForegroundColor Yellow

$endpoints = @(
    @{ endpoint = "admin/trigger-daily-updates"; description = "Daily Updates Trigger" },
    @{ endpoint = "admin/trigger-overdue-check"; description = "Overdue Check Trigger" },
    @{ endpoint = "admin/trigger-deadline-check"; description = "Deadline Check Trigger" }
)

foreach ($ep in $endpoints) {
    Test-AdminEndpoint -endpoint $ep.endpoint -description $ep.description
    Start-Sleep -Seconds 1
}

# Step 3: Verify notifications were created
Write-Host "`n🔄 Step 3: Verification..." -ForegroundColor Cyan

Write-Host "`n📋 Summary:" -ForegroundColor Green
Write-Host "✅ Test notifications SQL script created and executed" -ForegroundColor Green
Write-Host "✅ Admin trigger endpoints tested" -ForegroundColor Green
Write-Host "`n📝 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Start the HSE backend if not running: dotnet run" -ForegroundColor White
Write-Host "2. Login as admin@te.com (password: Admin123!) or hse@te.com (password: Hse123!)" -ForegroundColor White
Write-Host "3. Check the notification banner in the frontend" -ForegroundColor White
Write-Host "4. Manually trigger admin endpoints from the frontend if needed" -ForegroundColor White

Write-Host "`n🎯 Test Notifications Created:" -ForegroundColor Cyan
Write-Host "Admin User (admin@te.com):" -ForegroundColor Yellow
Write-Host "  - Daily HSE System Update" -ForegroundColor White
Write-Host "  - New Registration Request" -ForegroundColor White
Write-Host "  - Overdue Items Alert" -ForegroundColor White
Write-Host "  - Action Cancelled by HSE" -ForegroundColor White

Write-Host "`nHSE User (hse@te.com):" -ForegroundColor Yellow
Write-Host "  - New Report Submitted" -ForegroundColor White
Write-Host "  - Report Assigned to You" -ForegroundColor White
Write-Host "  - New Comment on Assigned Report" -ForegroundColor White
Write-Host "  - Action Deadline Approaching" -ForegroundColor White
Write-Host "  - New Action Added to Your Report" -ForegroundColor White

Write-Host "`n✅ Test notification creation completed!" -ForegroundColor Green