# HSE Test Notifications Setup

This guide explains how to create test notifications for the HSE system to test the notification banner functionality.

## Overview

The HSE system has a notification system that shows notifications to users via a banner/bell icon. To test this functionality, we need to create sample notifications for the seeded users.

## Seeded Users

The system comes with these pre-configured users:
- **admin@te.com** (password: Admin123!) - Admin role
- **hse@te.com** (password: Hse123!) - HSE role

## Files Created

### 1. TestNotificationsController.cs
A new controller that provides endpoints to create and manage test notifications:
- `POST /api/testnotifications/create-sample-notifications` - Creates sample notifications
- `GET /api/testnotifications/stats` - Shows notification statistics
- `DELETE /api/testnotifications/clear-all-notifications` - Clears all notifications

### 2. CreateTestNotifications.sql
A SQL script that directly inserts test notifications into the database.

### 3. TestNotificationEndpoints.ps1
A PowerShell script that tests the notification endpoints and creates sample data.

### 4. CreateTestNotifications.ps1
A PowerShell script that attempts to create notifications via multiple methods.

## Quick Setup Instructions

### Method 1: Using the Test Controller (Recommended)

1. **Start the HSE Backend:**
   ```bash
   cd HSEBackend
   dotnet run
   ```

2. **Run the test script:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File "TestNotificationEndpoints.ps1"
   ```

3. **Login to the frontend:**
   - Open the frontend application
   - Login as `admin@te.com` (password: `Admin123!`) or `hse@te.com` (password: `Hse123!`)
   - Check the notification banner/bell icon

### Method 2: Using Direct API Calls

If the backend is running, you can also call the endpoints directly:

```bash
# Create sample notifications
curl -X POST https://localhost:7068/api/testnotifications/create-sample-notifications

# Check statistics
curl -X GET https://localhost:7068/api/testnotifications/stats
```

### Method 3: Using Admin Trigger Endpoints

The system also has admin-only endpoints that can generate real notifications:

```bash
# These require admin authentication
POST /api/notifications/admin/trigger-daily-updates
POST /api/notifications/admin/trigger-overdue-check
POST /api/notifications/admin/trigger-deadline-check
```

## Test Notifications Created

### Admin User (admin@te.com)
- **Daily HSE System Update** (unread) - System statistics update
- **New Registration Request** (unread) - New user registration pending
- **Overdue Items Alert** (unread) - Items requiring attention
- **Action Cancelled by HSE** (read) - HSE user cancelled an action

### HSE User (hse@te.com)
- **New Report Submitted** (unread) - New incident report submitted
- **Report Assigned to You** (unread) - Report assigned by admin
- **New Comment on Assigned Report** (read) - Comment added to report
- **Action Deadline Approaching** (unread) - Action due soon
- **New Action Added to Your Report** (unread) - New action created

## Notification Types

The system supports these notification types:
- `DailyUpdate` - Daily system summaries
- `RegistrationRequest` - New user registration requests
- `OverdueAlert` - Overdue items alerts
- `ActionCancelled` - Action cancellation notifications
- `ReportSubmitted` - New report submissions
- `ReportAssigned` - Report assignments
- `CommentAdded` - New comments
- `DeadlineApproaching` - Upcoming deadlines
- `ActionAdded` - New actions created
- `ActionStatusChanged` - Action status updates
- `SubActionStatusChanged` - Sub-action status updates

## Troubleshooting

### Backend Not Running
If you get connection errors:
1. Make sure the backend is running: `dotnet run`
2. Check the URL in the scripts matches your backend URL
3. Verify HTTPS certificates are working

### No Notifications Appearing
If notifications don't appear in the frontend:
1. Check the browser console for errors
2. Verify you're logged in as the correct user
3. Check the notification API endpoint is working: `GET /api/notifications`
4. Run the stats endpoint to verify notifications exist: `GET /api/testnotifications/stats`

### Authentication Issues
The admin trigger endpoints require authentication:
1. Login to the frontend first
2. Use the authenticated session to call admin endpoints
3. Or call the test endpoints which don't require authentication

## Cleanup

To remove all test notifications:
```bash
curl -X DELETE https://localhost:7068/api/testnotifications/clear-all-notifications
```

## Frontend Integration

The notifications should appear in:
- Notification banner/dropdown
- Bell icon with unread count
- Notification list with read/unread status
- Different styling based on notification type (info, warning, error, success)

## Next Steps

After creating test notifications:
1. Test the notification banner functionality
2. Test marking notifications as read
3. Test notification filtering (unread only)
4. Test notification types and styling
5. Test real-time notification updates
6. Test email notifications (if configured)