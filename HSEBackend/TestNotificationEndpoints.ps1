# PowerShell script to test notification endpoints
# Run this after starting the HSE backend

Write-Host "Testing HSE Notification Endpoints" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# Configuration
$baseUrl = "https://localhost:7068"
$apiUrl = "$baseUrl/api"

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$method,
        [string]$endpoint,
        [string]$description
    )
    
    try {
        Write-Host "`nTesting: $description" -ForegroundColor Cyan
        Write-Host "Endpoint: $method $endpoint" -ForegroundColor Yellow
        
        if ($method -eq "GET") {
            $response = Invoke-RestMethod -Uri $endpoint -Method Get -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $endpoint -Method Post -ErrorAction Stop
        }
        
        Write-Host "✅ Success" -ForegroundColor Green
        Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
        return $true
    } catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
        return $false
    }
}

# Test if backend is running
Write-Host "`nChecking if backend is running..." -ForegroundColor Cyan
try {
    $healthCheck = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -ErrorAction Stop
    Write-Host "✅ Backend is running!" -ForegroundColor Green
} catch {
    Write-Host "❌ Backend is not running or not accessible at $baseUrl" -ForegroundColor Red
    Write-Host "Please start the backend with: dotnet run" -ForegroundColor Yellow
    Write-Host "Then run this script again." -ForegroundColor Yellow
    exit 1
}

# Test notification endpoints (these don't require authentication)
Write-Host "`n🔄 Testing Test Notification Endpoints..." -ForegroundColor Cyan

$endpoints = @(
    @{ method = "GET"; endpoint = "$apiUrl/testnotifications/stats"; description = "Get Notification Statistics" },
    @{ method = "POST"; endpoint = "$apiUrl/testnotifications/create-sample-notifications"; description = "Create Sample Notifications" },
    @{ method = "GET"; endpoint = "$apiUrl/testnotifications/stats"; description = "Get Updated Statistics" }
)

$successCount = 0
foreach ($ep in $endpoints) {
    if (Test-Endpoint -method $ep.method -endpoint $ep.endpoint -description $ep.description) {
        $successCount++
    }
    Start-Sleep -Seconds 1
}

Write-Host "`n📊 Results Summary:" -ForegroundColor Green
Write-Host "Successful requests: $successCount / $($endpoints.Count)" -ForegroundColor White

if ($successCount -eq $endpoints.Count) {
    Write-Host "`n🎉 All tests passed! Sample notifications have been created." -ForegroundColor Green
    Write-Host "`n📝 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Open the frontend application" -ForegroundColor White
    Write-Host "2. Login as admin@te.com (password: Admin123!) or hse@te.com (password: Hse123!)" -ForegroundColor White
    Write-Host "3. Check the notification banner/bell icon for new notifications" -ForegroundColor White
    Write-Host "4. You should see various test notifications created for both users" -ForegroundColor White
} else {
    Write-Host "`n⚠️ Some tests failed. Check the error messages above." -ForegroundColor Yellow
    Write-Host "Make sure the backend is running and the endpoints are accessible." -ForegroundColor Yellow
}

Write-Host "`n🔔 Expected Test Notifications:" -ForegroundColor Cyan
Write-Host "Admin User (admin@te.com):" -ForegroundColor Yellow
Write-Host "  - Daily HSE System Update (unread)" -ForegroundColor White
Write-Host "  - New Registration Request (unread)" -ForegroundColor White
Write-Host "  - Overdue Items Alert (unread)" -ForegroundColor White
Write-Host "  - Action Cancelled by HSE (read)" -ForegroundColor Gray

Write-Host "`nHSE User (hse@te.com):" -ForegroundColor Yellow
Write-Host "  - New Report Submitted (unread)" -ForegroundColor White
Write-Host "  - Report Assigned to You (unread)" -ForegroundColor White
Write-Host "  - New Comment on Assigned Report (read)" -ForegroundColor Gray
Write-Host "  - Action Deadline Approaching (unread)" -ForegroundColor White
Write-Host "  - New Action Added to Your Report (unread)" -ForegroundColor White

Write-Host "`n✅ Test notification setup completed!" -ForegroundColor Green