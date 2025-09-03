# HSEBackend API Testing Script
# PowerShell script for automated API testing

param(
    [string]$BaseUrl = "http://localhost:5225",
    [string]$JwtToken = "",
    [switch]$PublicOnly = $false,
    [switch]$Verbose = $false
)

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Blue = "Blue"

# Test counters
$script:PassedTests = 0
$script:FailedTests = 0
$script:TotalTests = 0

# Function to write colored output
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# Function to log test results
function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = "",
        [int]$StatusCode = 0
    )
    
    $script:TotalTests++
    
    if ($Passed) {
        $script:PassedTests++
        Write-ColorOutput "✓ PASS: $TestName" $Green
        if ($Verbose -and $Details) {
            Write-ColorOutput "  Details: $Details" "Gray"
        }
    } else {
        $script:FailedTests++
        Write-ColorOutput "✗ FAIL: $TestName" $Red
        if ($Details) {
            Write-ColorOutput "  Error: $Details" "Red"
        }
        if ($StatusCode -gt 0) {
            Write-ColorOutput "  Status Code: $StatusCode" "Red"
        }
    }
}

# Function to make HTTP request
function Invoke-APIRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [string]$Body = "",
        [int[]]$ExpectedStatusCodes = @(200)
    )
    
    try {
        $requestParams = @{
            Method = $Method
            Uri = $Uri
            Headers = $Headers
            ContentType = "application/json"
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $requestParams.Body = $Body
        }
        
        $response = Invoke-WebRequest @requestParams
        
        $success = $ExpectedStatusCodes -contains $response.StatusCode
        
        return @{
            Success = $success
            StatusCode = $response.StatusCode
            Content = $response.Content
            Headers = $response.Headers
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        $success = $ExpectedStatusCodes -contains $statusCode
        
        return @{
            Success = $success
            StatusCode = $statusCode
            Content = $_.Exception.Message
            Headers = @{}
            Error = $_.Exception.Message
        }
    }
}

# Function to test server connectivity
function Test-ServerConnectivity {
    Write-ColorOutput "`n=== Testing Server Connectivity ===" $Blue
    
    try {
        $response = Invoke-APIRequest -Method "GET" -Uri $BaseUrl -ExpectedStatusCodes @(200, 404)
        Write-TestResult "Server Connectivity" $response.Success "Server is reachable" $response.StatusCode
    }
    catch {
        Write-TestResult "Server Connectivity" $false "Server is not reachable: $($_.Exception.Message)"
    }
}

# Function to test public endpoints
function Test-PublicEndpoints {
    Write-ColorOutput "`n=== Testing Public Endpoints ===" $Blue
    
    # Test 1: Submit Valid Report
    $reportData = @{
        reporterId = "TE001"
        workShift = "Day"
        title = "Test Safety Incident Report"
        type = "Incident-Management"
        zone = "Production Area A"
        incidentDateTime = "2024-01-15T10:30:00"
        description = "Test incident description with sufficient detail to meet minimum requirements for validation."
        injuredPersonsCount = 1
        injuredPersons = @(
            @{
                name = "John Doe"
                department = "Production"
                zoneOfPerson = "Area A"
                gender = "Male"
                selectedBodyPart = "head"
                injuryType = "Cut"
                severity = "Minor"
                injuryDescription = "Small cut on forehead from equipment"
            }
        )
        immediateActionsTaken = "First aid applied, area secured, supervisor notified"
        actionStatus = "Completed"
        personInChargeOfActions = "Supervisor John Smith"
        dateActionsCompleted = "2024-01-15T11:00:00"
    } | ConvertTo-Json -Depth 5

    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body $reportData
    Write-TestResult "Submit Valid Report" $response.Success "Report submission" $response.StatusCode
    
    # Test 2: Submit Near Miss Report
    $nearMissData = @{
        reporterId = "TE002"
        workShift = "Afternoon"
        title = "Near Miss - Falling Object"
        type = "Near-Miss"
        zone = "Warehouse Section B"
        incidentDateTime = "2024-01-15T14:30:00"
        description = "A pallet nearly fell from the storage rack due to improper stacking. No injuries occurred but could have been serious."
        injuredPersonsCount = 0
        injuredPersons = @()
        immediateActionsTaken = "Area cordoned off, pallet repositioned, safety inspection scheduled"
        actionStatus = "In Progress"
        personInChargeOfActions = "Warehouse Manager"
        dateActionsCompleted = $null
    } | ConvertTo-Json -Depth 5

    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body $nearMissData
    Write-TestResult "Submit Near Miss Report" $response.Success "Near miss report submission" $response.StatusCode
    
    # Test 3: Submit Invalid Report (Should Fail)
    $invalidData = @{
        reporterId = ""
        workShift = ""
        title = ""
        type = ""
        zone = ""
        description = "Too short"
    } | ConvertTo-Json

    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body $invalidData -ExpectedStatusCodes @(400)
    Write-TestResult "Submit Invalid Report (Expected to Fail)" $response.Success "Validation should reject invalid data" $response.StatusCode
    
    # Test 4: Submit Registration Request
    $registrationData = @{
        fullName = "Jane Smith"
        companyId = "TE004"
        email = "jane.smith@te.com"
        department = "Quality Assurance"
    } | ConvertTo-Json

    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/register-request" -Body $registrationData
    Write-TestResult "Submit Registration Request" $response.Success "Registration request submission" $response.StatusCode
    
    # Test 5: Submit Duplicate Registration (Should Fail)
    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/register-request" -Body $registrationData -ExpectedStatusCodes @(400)
    Write-TestResult "Submit Duplicate Registration (Expected to Fail)" $response.Success "Should reject duplicate registration" $response.StatusCode
}

# Function to test authenticated endpoints
function Test-AuthenticatedEndpoints {
    Write-ColorOutput "`n=== Testing Authenticated Endpoints ===" $Blue
    
    if (-not $JwtToken) {
        Write-ColorOutput "JWT Token not provided. Skipping authenticated tests." $Yellow
        return
    }
    
    $authHeaders = @{
        "Authorization" = "Bearer $JwtToken"
        "Accept" = "application/json"
    }
    
    # Test 1: Get All Reports
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports" -Headers $authHeaders
    Write-TestResult "Get All Reports" $response.Success "Retrieve all reports" $response.StatusCode
    
    # Test 2: Get Reports with Filters
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports?type=Incident-Management&status=Pending" -Headers $authHeaders
    Write-TestResult "Get Reports with Filters" $response.Success "Retrieve filtered reports" $response.StatusCode
    
    # Test 3: Get Report Details
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports/1" -Headers $authHeaders
    Write-TestResult "Get Report Details" $response.Success "Retrieve specific report details" $response.StatusCode
    
    # Test 4: Get Non-existent Report
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports/9999" -Headers $authHeaders -ExpectedStatusCodes @(404)
    Write-TestResult "Get Non-existent Report (Expected 404)" $response.Success "Should return 404 for non-existent report" $response.StatusCode
    
    # Test 5: Update Report Status
    $statusData = @{
        status = "In Progress"
    } | ConvertTo-Json

    $response = Invoke-APIRequest -Method "PUT" -Uri "$BaseUrl/api/reports/1/status" -Headers $authHeaders -Body $statusData
    Write-TestResult "Update Report Status" $response.Success "Update report status" $response.StatusCode
    
    # Test 6: Add Comment to Report
    $commentData = @{
        content = "This incident has been reviewed by the HSE team. PowerShell automated test comment."
    } | ConvertTo-Json

    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports/1/comments" -Headers $authHeaders -Body $commentData
    Write-TestResult "Add Comment to Report" $response.Success "Add comment to report" $response.StatusCode
    
    # Test 7: Get Recent Reports
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports/recent?limit=5" -Headers $authHeaders
    Write-TestResult "Get Recent Reports" $response.Success "Retrieve recent reports" $response.StatusCode
    
    # Test 8: Get All Registration Requests
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/register-request" -Headers $authHeaders
    Write-TestResult "Get All Registration Requests" $response.Success "Retrieve all registration requests" $response.StatusCode
    
    # Test 9: Get Pending Users
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/pending-users" -Headers $authHeaders
    Write-TestResult "Get Pending Users" $response.Success "Retrieve pending users" $response.StatusCode
}

# Function to test authentication
function Test-Authentication {
    Write-ColorOutput "`n=== Testing Authentication ===" $Blue
    
    # Test 1: Access Protected Endpoint Without Token
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports" -ExpectedStatusCodes @(401)
    Write-TestResult "Access Protected Endpoint Without Token (Expected 401)" $response.Success "Should return 401 unauthorized" $response.StatusCode
    
    # Test 2: Access Protected Endpoint With Invalid Token
    $invalidAuthHeaders = @{
        "Authorization" = "Bearer invalid_token_here"
        "Accept" = "application/json"
    }
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports" -Headers $invalidAuthHeaders -ExpectedStatusCodes @(401)
    Write-TestResult "Access Protected Endpoint With Invalid Token (Expected 401)" $response.Success "Should return 401 unauthorized" $response.StatusCode
    
    # Test 3: Access Protected Endpoint With Malformed Token
    $malformedAuthHeaders = @{
        "Authorization" = "Bearer malformed.jwt.token"
        "Accept" = "application/json"
    }
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/reports" -Headers $malformedAuthHeaders -ExpectedStatusCodes @(401)
    Write-TestResult "Access Protected Endpoint With Malformed Token (Expected 401)" $response.Success "Should return 401 unauthorized" $response.StatusCode
}

# Function to test error handling
function Test-ErrorHandling {
    Write-ColorOutput "`n=== Testing Error Handling ===" $Blue
    
    # Test 1: Invalid Endpoint
    $response = Invoke-APIRequest -Method "GET" -Uri "$BaseUrl/api/invalid-endpoint" -ExpectedStatusCodes @(404)
    Write-TestResult "Invalid Endpoint (Expected 404)" $response.Success "Should return 404 for invalid endpoint" $response.StatusCode
    
    # Test 2: Invalid HTTP Method
    $response = Invoke-APIRequest -Method "PATCH" -Uri "$BaseUrl/api/reports" -ExpectedStatusCodes @(405, 404)
    Write-TestResult "Invalid HTTP Method (Expected 405)" $response.Success "Should return 405 for invalid method" $response.StatusCode
    
    # Test 3: Invalid JSON
    $invalidJson = '{"reporterId": "TE001", "workShift": "Day", "title": "Test", "type": "Incident-Management", "zone": "Area A", "incidentDateTime": "invalid-date", "description": "Test", "injuredPersonsCount": "not-a-number"}'
    $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body $invalidJson -ExpectedStatusCodes @(400)
    Write-TestResult "Invalid JSON Data (Expected 400)" $response.Success "Should return 400 for invalid JSON data" $response.StatusCode
    
    # Test 4: Missing Content-Type
    try {
        $response = Invoke-WebRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body '{"test": "data"}' -UseBasicParsing
        Write-TestResult "Missing Content-Type Header" $false "Should handle missing content-type" $response.StatusCode
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        Write-TestResult "Missing Content-Type Header" $true "Properly handled missing content-type" $statusCode
    }
}

# Function to test performance
function Test-Performance {
    Write-ColorOutput "`n=== Testing Performance ===" $Blue
    
    $performanceTests = @()
    
    # Test multiple requests
    for ($i = 1; $i -le 5; $i++) {
        $startTime = Get-Date
        
        $testData = @{
            reporterId = "TE00$i"
            workShift = "Day"
            title = "Performance Test Report $i"
            type = "Near-Miss"
            zone = "Test Area"
            incidentDateTime = "2024-01-15T10:30:00"
            description = "Performance test report number $i for load testing purposes"
            injuredPersonsCount = 0
            injuredPersons = @()
        } | ConvertTo-Json -Depth 5
        
        $response = Invoke-APIRequest -Method "POST" -Uri "$BaseUrl/api/reports" -Body $testData
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalMilliseconds
        
        $performanceTests += @{
            TestNumber = $i
            Duration = $duration
            Success = $response.Success
            StatusCode = $response.StatusCode
        }
        
        Write-TestResult "Performance Test $i" $response.Success "Response time: $($duration)ms" $response.StatusCode
    }
    
    # Calculate average response time
    $avgResponseTime = ($performanceTests | Measure-Object -Property Duration -Average).Average
    $maxResponseTime = ($performanceTests | Measure-Object -Property Duration -Maximum).Maximum
    $minResponseTime = ($performanceTests | Measure-Object -Property Duration -Minimum).Minimum
    
    Write-ColorOutput "Performance Summary:" $Yellow
    Write-ColorOutput "  Average Response Time: $([math]::Round($avgResponseTime, 2))ms" "White"
    Write-ColorOutput "  Maximum Response Time: $([math]::Round($maxResponseTime, 2))ms" "White"
    Write-ColorOutput "  Minimum Response Time: $([math]::Round($minResponseTime, 2))ms" "White"
    
    # Performance thresholds
    $performancePass = $avgResponseTime -lt 2000 -and $maxResponseTime -lt 5000
    Write-TestResult "Performance Thresholds" $performancePass "Average < 2000ms, Max < 5000ms"
}

# Main execution
function Main {
    Write-ColorOutput "HSEBackend API Testing Script" $Blue
    Write-ColorOutput "=============================" $Blue
    Write-ColorOutput "Base URL: $BaseUrl" "White"
    Write-ColorOutput "JWT Token: $($JwtToken -ne '' ? 'Provided' : 'Not Provided')" "White"
    Write-ColorOutput "Public Only: $PublicOnly" "White"
    Write-ColorOutput "Verbose: $Verbose" "White"
    Write-ColorOutput ""
    
    # Test server connectivity first
    Test-ServerConnectivity
    
    # Test public endpoints
    Test-PublicEndpoints
    
    if (-not $PublicOnly) {
        # Test authentication
        Test-Authentication
        
        # Test authenticated endpoints if token is provided
        if ($JwtToken) {
            Test-AuthenticatedEndpoints
        }
        
        # Test error handling
        Test-ErrorHandling
        
        # Test performance
        Test-Performance
    }
    
    # Print summary
    Write-ColorOutput "`n=== Test Summary ===" $Blue
    Write-ColorOutput "Total Tests: $script:TotalTests" "White"
    Write-ColorOutput "Passed: $script:PassedTests" $Green
    Write-ColorOutput "Failed: $script:FailedTests" $Red
    Write-ColorOutput "Success Rate: $([math]::Round(($script:PassedTests / $script:TotalTests) * 100, 2))%" "White"
    
    if ($script:FailedTests -eq 0) {
        Write-ColorOutput "`n🎉 All tests passed!" $Green
        exit 0
    } else {
        Write-ColorOutput "`n❌ Some tests failed. Please review the results above." $Red
        exit 1
    }
}

# Run the main function
Main