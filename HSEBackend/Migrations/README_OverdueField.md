# Overdue Field Implementation

## Overview
This migration adds the `Overdue` field to track when actions, corrective actions, and sub-actions have passed their deadlines.

## Database Changes

### Tables Modified:
1. **Actions** - Added `Overdue BIT NOT NULL DEFAULT 0`
2. **CorrectiveActions** - Added `Overdue BIT NOT NULL DEFAULT 0`  
3. **SubActions** - Added `Overdue BIT NOT NULL DEFAULT 0`

### Migration Steps:
1. Run the SQL script: `Manual_AddOverdueField.sql`
2. Or use the batch file: `apply_migration.bat`

## Backend Changes

### Models Updated:
- `Action.cs` - Added `public bool Overdue { get; set; } = false;`
- `CorrectiveAction.cs` - Added `public bool Overdue { get; set; } = false;`
- `SubAction.cs` - Added `public bool Overdue { get; set; } = false;`

### DTOs Updated:
- `ActionDetailDto` - Added `public bool Overdue { get; set; } = false;`
- `SubActionDetailDto` - Added `public bool Overdue { get; set; } = false;`
- `CorrectiveActionDetailDto` - Added `public bool Overdue { get; set; } = false;`
- `CorrectiveActionSummaryDto` - Added `public bool Overdue { get; set; } = false;`

### Services Added:
- `OverdueService` - Manages overdue status updates
- `OverdueBackgroundService` - Automatically updates overdue status every hour

### Controllers Updated:
- `ActionsController` - Returns overdue field in responses
- `CorrectiveActionsController` - Returns overdue field in responses

## Frontend Changes

### Actions Page Improvements:
1. **Enhanced KPIs**: 6 KPI cards showing Total, Completed, In Progress, Not Started, Overdue, and Canceled/Aborted
2. **Priority Colors**: Priority badges with color coding (Critical=Red, High=Orange, Medium=Yellow, Low=Green)
3. **Improved Layout**: Clean action cards with proper information hierarchy
4. **Admin Features**: Author information visible only to admin users
5. **Status Display**: Status shown as read-only badges (not dropdown)
6. **Overdue Indicators**: Visual warnings for overdue items

### Action Card Layout:
```
┌─────────────────────────────────────────────────────────┐
│ [Priority Badge] Hierarchy               Created: Date  │
│ 📄 Report #123 - Report Title                          │
│                                                         │
│ Action Title                                            │
│ Action description text here...                         │
│                                                         │
│ 👤 Author: Name (Admin only)                           │
│                                                         │
│ 👤 Assignee  [Status Badge] ⚠  📅 Deadline  [▼ Sub(2)] │
└─────────────────────────────────────────────────────────┘
```

### Sub-action Cards:
- Title with overdue warning
- Description  
- Assigned user
- Deadline
- Cancel button only (no edit/view)

## Logic Implementation

### Overdue Calculation:
```sql
Overdue = 1 WHERE 
  DueDate < GETDATE() AND 
  Status NOT IN ('Completed', 'Canceled', 'Aborted')
```

### Auto-Reset:
```sql  
Overdue = 0 WHERE
  Status IN ('Completed', 'Canceled', 'Aborted')
```

### Background Processing:
- Runs every hour
- Updates all overdue statuses automatically
- Logs changes for monitoring

## How to Apply Migration

### Option 1: SQL Server Management Studio
1. Open `Manual_AddOverdueField.sql`
2. Connect to your HSE database
3. Execute the script

### Option 2: Command Line
1. Open Command Prompt
2. Navigate to the Migrations folder
3. Run `apply_migration.bat`

### Option 3: Entity Framework (when backend is not running)
```bash
cd HSEBackend
dotnet ef migrations add AddOverdueField
dotnet ef database update
```

## Verification

After applying the migration, verify:
1. ✅ Tables have `Overdue` column
2. ✅ Backend compiles and runs
3. ✅ Frontend displays real data
4. ✅ KPIs show correct counts
5. ✅ Action cards show improved layout
6. ✅ Overdue items are highlighted

## Impact

### Benefits:
- ✅ Persistent overdue tracking in database
- ✅ Automatic overdue status management
- ✅ Enhanced user interface with better information display
- ✅ Role-based access (admin sees author info)
- ✅ Simplified sub-action management
- ✅ All action types displayed (including aborted)

### Performance:
- Background service runs hourly (low impact)
- Database indexes on DueDate recommended for large datasets
- Efficient queries with status filtering