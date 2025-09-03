# Report Status Migration Instructions

The report status names have been updated in the application code from:
- `"Open"` → `"Unopened"`
- `"In Progress"` → `"Opened"`
- `"Closed"` remains `"Closed"`

However, existing reports in the database still use the old status names, which is why the dashboard indicators show 0. You need to run a migration to update the existing data.

## Option 1: Using the Migration API Endpoint (Recommended)

1. **Restart the backend** first to pick up the new MigrationController
2. **Login as an Admin user** in the frontend
3. **Call the migration endpoint** using one of these methods:

### Using Browser/Postman:
```
GET http://localhost:5000/api/migration/migration-status
```
This will check if migration is needed.

```
POST http://localhost:5000/api/migration/update-report-statuses
```
This will perform the migration.

### Using Browser Console:
1. Open the frontend application in your browser
2. Login as an Admin user
3. Open browser developer tools (F12)
4. In the Console tab, run:

```javascript
// Check if migration is needed
fetch('/api/migration/migration-status', {
    headers: {
        'Authorization': 'Bearer ' + localStorage.getItem('authToken')
    }
})
.then(r => r.json())
.then(console.log);

// If migration is needed, run it:
fetch('/api/migration/update-report-statuses', {
    method: 'POST',
    headers: {
        'Authorization': 'Bearer ' + localStorage.getItem('authToken')
    }
})
.then(r => r.json())
.then(console.log);
```

## Option 2: Using SQL Script (Alternative)

If you prefer to run SQL directly:

1. **Stop the backend application** first
2. **Navigate to the HSEBackend/Scripts folder**
3. **Run the PowerShell script:**
   ```powershell
   .\RunStatusMigration.ps1
   ```
   
   Or run the SQL script directly if you have sqlite3 installed:
   ```bash
   sqlite3 HSE_Database.db < UpdateReportStatuses.sql
   ```

## Verification

After running the migration, you can verify it worked by:

1. **Checking the API endpoint:**
   ```
   GET http://localhost:5000/api/migration/report-status-counts
   ```

2. **Refreshing the dashboard** - you should now see the correct status indicators with real counts

3. **Checking the reports list** - status badges should show proper values

## Expected Results

After migration, you should see:
- Dashboard shows "Unopened Reports" with the count of previously "Open" reports
- Reports list shows status badges with "Unopened", "Opened", and "Closed"
- All indicators connect to actual backend data

## Files Created

- `HSEBackend/Controllers/MigrationController.cs` - API endpoints for migration
- `HSEBackend/Scripts/UpdateReportStatuses.sql` - SQL migration script
- `HSEBackend/Scripts/RunStatusMigration.ps1` - PowerShell script to run SQL migration

## Important Notes

- Only Admin users can access the migration endpoints
- The migration is safe and includes rollback on errors
- All changes are logged for tracking
- The migration updates the `UpdatedAt` timestamp for all modified reports