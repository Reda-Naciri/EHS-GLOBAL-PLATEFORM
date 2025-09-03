# Simple PowerShell script to test notification endpoints
# Run this after starting the HSE backend

Write-Host "Testing HSE Notification Endpoints"
Write-Host "==================================="

# Configuration
$baseUrl = "http://localhost:5225"
$apiUrl = "$baseUrl/api"

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$method,
        [string]$endpoint,
        [string]$description
    )
    
    try {
        Write-Host ""
        Write-Host "Testing: $description"
        Write-Host "Endpoint: $method $endpoint"
        
        if ($method -eq "GET") {
            $response = Invoke-RestMethod -Uri $endpoint -Method Get -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $endpoint -Method Post -ErrorAction Stop
        }
        
        Write-Host "SUCCESS"
        Write-Host "Response: $($response | ConvertTo-Json -Depth 2)"
        return $true
    } catch {
        Write-Host "FAILED: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
        }
        return $false
    }
}

# Test if backend is running
Write-Host ""
Write-Host "Checking if backend is running..."
try {
    $healthCheck = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -ErrorAction Stop
    Write-Host "Backend is running!"
} catch {
    Write-Host "Backend is not running or not accessible at $baseUrl"
    Write-Host "Please start the backend with: dotnet run"
    Write-Host "Then run this script again."
    exit 1
}

# Test notification endpoints
Write-Host ""
Write-Host "Testing Test Notification Endpoints..."

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

Write-Host ""
Write-Host "Results Summary:"
Write-Host "Successful requests: $successCount / $($endpoints.Count)"

if ($successCount -eq $endpoints.Count) {
    Write-Host ""
    Write-Host "All tests passed! Sample notifications have been created."
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "1. Open the frontend application"
    Write-Host "2. Login as admin@te.com (password: Admin123!) or hse@te.com (password: Hse123!)"
    Write-Host "3. Check the notification banner/bell icon for new notifications"
    Write-Host "4. You should see various test notifications created for both users"
} else {
    Write-Host ""
    Write-Host "Some tests failed. Check the error messages above."
    Write-Host "Make sure the backend is running and the endpoints are accessible."
}

Write-Host ""
Write-Host "Expected Test Notifications:"
Write-Host "Admin User (admin@te.com):"
Write-Host "  - Daily HSE System Update (unread)"
Write-Host "  - New Registration Request (unread)"
Write-Host "  - Overdue Items Alert (unread)"
Write-Host "  - Action Cancelled by HSE (read)"

Write-Host ""
Write-Host "HSE User (hse@te.com):"
Write-Host "  - New Report Submitted (unread)"
Write-Host "  - Report Assigned to You (unread)"
Write-Host "  - New Comment on Assigned Report (read)"
Write-Host "  - Action Deadline Approaching (unread)"
Write-Host "  - New Action Added to Your Report (unread)"

Write-Host ""
Write-Host "Test notification setup completed!"