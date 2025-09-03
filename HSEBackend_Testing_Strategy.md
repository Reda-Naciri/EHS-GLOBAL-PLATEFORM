# HSEBackend API Testing Strategy

## Overview
This document provides a comprehensive testing strategy for the HSEBackend API, including test cases, sample payloads, and testing tools recommendations.

## Backend Architecture Summary
- **Authentication**: JWT-based authentication with roles (HSE, Admin, Profil)
- **Database**: SQL Server with Entity Framework Core
- **Main Controllers**: ReportsController, PendingUsersController, RegistrationRequestController
- **Base URL**: http://localhost:5225

## Available Endpoints

### 1. Reports API (`/api/reports`)
- **POST** `/api/reports` - Submit report (Public)
- **GET** `/api/reports` - Get all reports (HSE/Admin only)
- **GET** `/api/reports/{id}` - Get report details (HSE/Admin only)
- **PUT** `/api/reports/{id}/status` - Update report status (HSE/Admin only)
- **POST** `/api/reports/{id}/comments` - Add comment (HSE/Admin only)
- **GET** `/api/reports/recent` - Get recent reports (HSE/Admin only)

### 2. Registration Requests API (`/api/register-request`)
- **POST** `/api/register-request` - Submit registration request (Public)
- **GET** `/api/register-request` - Get all requests (HSE/Admin only)
- **PUT** `/api/register-request/{id}/approve` - Approve request (HSE/Admin only)
- **PUT** `/api/register-request/{id}/reject` - Reject request (HSE/Admin only)

### 3. Pending Users API (`/api/pending-users`)
- **GET** `/api/pending-users` - Get pending users (HSE/Admin only)

## Testing Tools Recommendations

### 1. **Postman** (Recommended)
- **Pros**: User-friendly GUI, collection management, environment variables, automated testing
- **Cons**: Requires installation
- **Best for**: Manual testing, API documentation, sharing test collections

### 2. **REST Client (VS Code Extension)**
- **Pros**: Integrated in VS Code, simple HTTP files, version control friendly
- **Cons**: Limited GUI features
- **Best for**: Developers who prefer file-based testing

### 3. **curl** (Command Line)
- **Pros**: Universal, scriptable, no installation needed
- **Cons**: Complex syntax for complex requests
- **Best for**: Quick tests, CI/CD integration, scripting

### 4. **Swagger UI**
- **Pros**: Auto-generated, interactive documentation
- **Cons**: Limited test data management
- **Best for**: API exploration, initial testing

## Test Environment Setup

### Prerequisites
1. Backend server running on `http://localhost:5225`
2. SQL Server database accessible
3. Valid JWT tokens for authenticated endpoints

### Environment Variables
```bash
# Base URL
BASE_URL=http://localhost:5225

# JWT Token (for authenticated requests)
JWT_TOKEN=your_jwt_token_here

# Test User Credentials
TEST_EMAIL=test@te.com
TEST_COMPANY_ID=TE001
```

## Test Cases

### A. Public Endpoints (No Authentication Required)

#### 1. Submit Report
**Endpoint**: `POST /api/reports`

**Test Case 1: Valid Report Submission**
```bash
curl -X POST http://localhost:5225/api/reports \
  -H "Content-Type: application/json" \
  -d '{
    "reporterId": "TE001",
    "workShift": "Day",
    "title": "Test Safety Report",
    "type": "Incident-Management",
    "zone": "Production Area A",
    "incidentDateTime": "2024-01-15T10:30:00",
    "description": "Test incident description with sufficient detail to meet minimum requirements.",
    "injuredPersonsCount": 1,
    "injuredPersons": [
      {
        "name": "John Doe",
        "department": "Production",
        "zoneOfPerson": "Area A",
        "gender": "Male",
        "selectedBodyPart": "head",
        "injuryType": "Cut",
        "severity": "Minor",
        "injuryDescription": "Small cut on forehead"
      }
    ],
    "immediateActionsTaken": "First aid applied, area secured",
    "actionStatus": "Completed",
    "personInChargeOfActions": "Supervisor John",
    "dateActionsCompleted": "2024-01-15T11:00:00"
  }'
```

**Expected Response**: 200 OK with report ID and success message

**Test Case 2: Invalid Report (Missing Required Fields)**
```bash
curl -X POST http://localhost:5225/api/reports \
  -H "Content-Type: application/json" \
  -d '{
    "reporterId": "",
    "workShift": "",
    "title": "",
    "type": "",
    "zone": "",
    "description": "Too short"
  }'
```

**Expected Response**: 400 Bad Request with validation errors

#### 2. Submit Registration Request
**Endpoint**: `POST /api/register-request`

**Test Case 3: Valid Registration Request**
```bash
curl -X POST http://localhost:5225/api/register-request \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Jane Smith",
    "companyId": "TE002",
    "email": "jane.smith@te.com",
    "department": "Quality Assurance"
  }'
```

**Expected Response**: 200 OK with success message

**Test Case 4: Duplicate Registration Request**
```bash
# Submit the same request twice
curl -X POST http://localhost:5225/api/register-request \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Jane Smith",
    "companyId": "TE002",
    "email": "jane.smith@te.com",
    "department": "Quality Assurance"
  }'
```

**Expected Response**: 400 Bad Request with duplicate message

### B. Authenticated Endpoints (JWT Required)

#### Authentication Setup
First, you need to obtain a JWT token. Since there's no auth endpoint visible, you'll need to:
1. Create a test user in the database
2. Use Identity's token generation
3. Or create a test authentication endpoint

**Sample JWT Header for all authenticated requests:**
```bash
-H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

#### 3. Get All Reports
**Endpoint**: `GET /api/reports`

**Test Case 5: Get All Reports (No Filters)**
```bash
curl -X GET http://localhost:5225/api/reports \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

**Test Case 6: Get Reports with Filters**
```bash
curl -X GET "http://localhost:5225/api/reports?type=Incident-Management&status=Pending&zone=Production" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

**Expected Response**: 200 OK with array of reports

#### 4. Get Report Details
**Endpoint**: `GET /api/reports/{id}`

**Test Case 7: Get Existing Report**
```bash
curl -X GET http://localhost:5225/api/reports/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

**Test Case 8: Get Non-existent Report**
```bash
curl -X GET http://localhost:5225/api/reports/9999 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

**Expected Response**: 404 Not Found

#### 5. Update Report Status
**Endpoint**: `PUT /api/reports/{id}/status`

**Test Case 9: Update Report Status**
```bash
curl -X PUT http://localhost:5225/api/reports/1/status \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "In Progress"
  }'
```

#### 6. Add Comment to Report
**Endpoint**: `POST /api/reports/{id}/comments`

**Test Case 10: Add Comment**
```bash
curl -X POST http://localhost:5225/api/reports/1/comments \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "This is a test comment from HSE team"
  }'
```

#### 7. Get Recent Reports
**Endpoint**: `GET /api/reports/recent`

**Test Case 11: Get Recent Reports**
```bash
curl -X GET "http://localhost:5225/api/reports/recent?limit=5" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

#### 8. Registration Request Management
**Endpoint**: `GET /api/register-request`

**Test Case 12: Get All Registration Requests**
```bash
curl -X GET http://localhost:5225/api/register-request \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

**Test Case 13: Approve Registration Request**
```bash
curl -X PUT http://localhost:5225/api/register-request/1/approve \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

**Test Case 14: Reject Registration Request**
```bash
curl -X PUT http://localhost:5225/api/register-request/1/reject \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

#### 9. Get Pending Users
**Endpoint**: `GET /api/pending-users`

**Test Case 15: Get Pending Users**
```bash
curl -X GET http://localhost:5225/api/pending-users \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Accept: application/json"
```

## Authentication and Authorization Testing

### Testing Authentication
1. **No Token**: Test all protected endpoints without JWT token
2. **Invalid Token**: Test with malformed or expired JWT token
3. **Valid Token**: Test with proper JWT token

### Testing Authorization
1. **Wrong Role**: Test HSE/Admin endpoints with 'Profil' role token
2. **Correct Role**: Test with appropriate role permissions

### Sample Authentication Test Cases
```bash
# Test without token (should return 401)
curl -X GET http://localhost:5225/api/reports

# Test with invalid token (should return 401)
curl -X GET http://localhost:5225/api/reports \
  -H "Authorization: Bearer invalid_token_here"

# Test with valid token but wrong role (should return 403)
curl -X GET http://localhost:5225/api/reports \
  -H "Authorization: Bearer PROFIL_ROLE_TOKEN"
```

## Database Operations Testing

### 1. Database Connection Testing
- Test with database server down
- Test with invalid connection string
- Test with database permissions issues

### 2. Data Integrity Testing
- Test foreign key constraints
- Test required field validations
- Test data type validations

### 3. Transaction Testing
- Test concurrent report submissions
- Test rollback scenarios

## Systematic Testing Approach

### Phase 1: Basic Connectivity
1. Test server startup
2. Test database connection
3. Test Swagger UI access

### Phase 2: Public Endpoints
1. Test all public endpoints with valid data
2. Test all public endpoints with invalid data
3. Test edge cases and boundary conditions

### Phase 3: Authentication System
1. Test JWT token generation (if endpoint exists)
2. Test token validation
3. Test role-based access control

### Phase 4: Protected Endpoints
1. Test all protected endpoints with valid authentication
2. Test authorization for different roles
3. Test business logic validation

### Phase 5: Integration Testing
1. Test complete workflows (registration → approval → report submission)
2. Test email notifications
3. Test file upload/download (if implemented)

### Phase 6: Performance Testing
1. Test concurrent users
2. Test large data sets
3. Test memory usage

## Testing Data Management

### Sample Test Data
```json
{
  "testReporter": {
    "reporterId": "TE001",
    "name": "Test Reporter",
    "department": "Testing",
    "zone": "Test Area"
  },
  "testIncident": {
    "title": "Test Safety Incident",
    "type": "Incident-Management",
    "zone": "Production Area A",
    "description": "This is a test incident report with sufficient detail to meet validation requirements.",
    "workShift": "Day"
  },
  "testInjuredPerson": {
    "name": "Test Person",
    "department": "Production",
    "zoneOfPerson": "Area A",
    "gender": "Male",
    "selectedBodyPart": "head",
    "injuryType": "Cut",
    "severity": "Minor",
    "injuryDescription": "Test injury description"
  }
}
```

## Error Handling Testing

### Expected Error Responses
1. **400 Bad Request**: Invalid input data
2. **401 Unauthorized**: Missing or invalid JWT token
3. **403 Forbidden**: Valid token but insufficient permissions
4. **404 Not Found**: Resource not found
5. **500 Internal Server Error**: Server-side errors

### Test Invalid Scenarios
- Malformed JSON
- Missing required fields
- Invalid data types
- SQL injection attempts
- XSS attempts

## Performance and Load Testing

### Recommended Tools
1. **Artillery**: For load testing
2. **Apache Bench (ab)**: For simple load tests
3. **k6**: For performance testing

### Sample Load Test
```bash
# Simple load test with curl
for i in {1..10}; do
  curl -X POST http://localhost:5225/api/reports \
    -H "Content-Type: application/json" \
    -d '@test_report.json' &
done
wait
```

## Continuous Integration Testing

### Automated Test Scripts
Create shell scripts or PowerShell scripts to run all test cases:

```bash
#!/bin/bash
# test_all_endpoints.sh

BASE_URL="http://localhost:5225"
JWT_TOKEN="your_token_here"

echo "Testing public endpoints..."
# Run all public endpoint tests

echo "Testing authenticated endpoints..."
# Run all authenticated endpoint tests

echo "Testing complete!"
```

## Monitoring and Logging

### Test Logging
- Enable detailed logging in appsettings.json
- Monitor application logs during testing
- Check database logs for errors

### Health Checks
- Implement health check endpoints
- Monitor system resources during testing
- Track response times and error rates

## Conclusion

This comprehensive testing strategy covers all aspects of the HSEBackend API testing. Start with basic connectivity tests, then progress through each phase systematically. Use the provided test cases as a foundation and expand them based on your specific requirements and discovered edge cases.

Remember to:
1. Test both positive and negative scenarios
2. Validate all input parameters
3. Check authentication and authorization thoroughly
4. Monitor database operations
5. Test email notifications
6. Verify error handling and logging
7. Test performance under load

For best results, combine manual testing with automated test scripts and integrate testing into your CI/CD pipeline.