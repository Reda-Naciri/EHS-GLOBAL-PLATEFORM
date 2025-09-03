# PowerShell script to query SQLite database
param(
    [string]$DatabasePath = "HSEBackend\HSE_DB.db"
)

$ErrorActionPreference = "Stop"

Write-Host "=== HSE Database Investigation ===" -ForegroundColor Yellow
Write-Host "Database: $DatabasePath" -ForegroundColor Gray
Write-Host ""

try {
    # Load SQLite assembly
    Add-Type -AssemblyName System.Data
    
    # Connection string
    $connectionString = "Data Source=$DatabasePath;Version=3;"
    
    # Create connection
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Database connection successful" -ForegroundColor Green
    Write-Host ""
    
    # Function to execute query and display results
    function Execute-Query {
        param([string]$query, [string]$title)
        
        Write-Host "=== $title ===" -ForegroundColor Cyan
        
        $command = $connection.CreateCommand()
        $command.CommandText = $query
        $reader = $command.ExecuteReader()
        
        # Get column names
        $columns = @()
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $columns += $reader.GetName($i)
        }
        
        # Display header
        $header = $columns -join " | "
        Write-Host $header -ForegroundColor White
        Write-Host ("-" * $header.Length) -ForegroundColor White
        
        # Display rows
        $rowCount = 0
        while ($reader.Read()) {
            $values = @()
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $value = $reader.GetValue($i)
                if ($value -eq [DBNull]::Value) {
                    $values += "NULL"
                } else {
                    $values += $value.ToString()
                }
            }
            Write-Host ($values -join " | ")
            $rowCount++
        }
        
        $reader.Close()
        Write-Host ""
        Write-Host "Total rows: $rowCount" -ForegroundColor Gray
        Write-Host ""
    }
    
    # 1. First, get all users to find Yahya
    $usersQuery = @"
SELECT Id, FirstName, LastName, Email, CompanyId 
FROM AspNetUsers 
ORDER BY LastName, FirstName
"@
    Execute-Query -query $usersQuery -title "All Users in Database"
    
    # 2. Find users with 'Yahya' in name or email
    $yahyaQuery = @"
SELECT Id, FirstName, LastName, Email, CompanyId 
FROM AspNetUsers 
WHERE FirstName LIKE '%Yahya%' OR LastName LIKE '%Yahya%' OR Email LIKE '%yahya%'
"@
    Execute-Query -query $yahyaQuery -title "Users matching 'Yahya'"
    
    # 3. Get all CorrectiveActions with creator info
    $allActionsQuery = @"
SELECT 
    ca.Id,
    ca.Title,
    ca.Status,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt,
    ca.ReportId
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
ORDER BY ca.Id
"@
    Execute-Query -query $allActionsQuery -title "All Corrective Actions"
    
    # 4. Check specific IDs mentioned in logs [42, 43, 45, 46]
    $specificActionsQuery = @"
SELECT 
    ca.Id,
    ca.Title,
    ca.Status,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE ca.Id IN (42, 43, 45, 46)
ORDER BY ca.Id
"@
    Execute-Query -query $specificActionsQuery -title "Specific Actions from Logs [42, 43, 45, 46]"
    
    # 5. Get actions created by anyone with 'Yahya' in their name
    $yahyaActionsQuery = @"
SELECT 
    ca.Id,
    ca.Title,
    ca.Status,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt,
    ca.ReportId
FROM CorrectiveActions ca
INNER JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE u.FirstName LIKE '%Yahya%' OR u.LastName LIKE '%Yahya%' OR u.Email LIKE '%yahya%'
ORDER BY ca.Id
"@
    Execute-Query -query $yahyaActionsQuery -title "Actions Created by Yahya"
    
    # 6. Count actions by creator
    $countByCreatorQuery = @"
SELECT 
    u.FirstName || ' ' || u.LastName as CreatorName,
    u.Email,
    COUNT(ca.Id) as ActionCount
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
GROUP BY ca.CreatedByHSEId, u.FirstName, u.LastName, u.Email
ORDER BY ActionCount DESC, CreatorName
"@
    Execute-Query -query $countByCreatorQuery -title "Action Count by Creator"
    
    $connection.Close()
    Write-Host "✅ Database connection closed" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try alternative approach without SQLite assembly
    Write-Host ""
    Write-Host "Trying alternative approach..." -ForegroundColor Yellow
    
    try {
        # Check if sqlite3.exe is available in PATH or try to download it
        $sqlite3Path = Get-Command sqlite3.exe -ErrorAction SilentlyContinue
        
        if (-not $sqlite3Path) {
            Write-Host "SQLite3 command line tool not found in PATH" -ForegroundColor Red
            Write-Host "Please install SQLite3 or use alternative approach" -ForegroundColor Yellow
        } else {
            Write-Host "Found SQLite3 at: $($sqlite3Path.Source)" -ForegroundColor Green
            
            # Create SQL queries file
            $sqlFile = "temp_queries.sql"
            $queries = @"
.headers on
.mode column

-- All Users
.print "=== All Users in Database ==="
SELECT Id, FirstName, LastName, Email, CompanyId FROM AspNetUsers ORDER BY LastName, FirstName;

.print ""
.print "=== Users matching 'Yahya' ==="
SELECT Id, FirstName, LastName, Email, CompanyId FROM AspNetUsers WHERE FirstName LIKE '%Yahya%' OR LastName LIKE '%Yahya%' OR Email LIKE '%yahya%';

.print ""
.print "=== All Corrective Actions ==="
SELECT ca.Id, ca.Title, ca.Status, ca.CreatedByHSEId, u.FirstName || ' ' || u.LastName as CreatedByName, u.Email as CreatedByEmail, ca.CreatedAt, ca.ReportId
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
ORDER BY ca.Id;

.print ""
.print "=== Actions from Logs [42, 43, 45, 46] ==="
SELECT ca.Id, ca.Title, ca.Status, ca.CreatedByHSEId, u.FirstName || ' ' || u.LastName as CreatedByName, u.Email as CreatedByEmail, ca.CreatedAt
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE ca.Id IN (42, 43, 45, 46)
ORDER BY ca.Id;

.print ""
.print "=== Actions Created by Yahya ==="
SELECT ca.Id, ca.Title, ca.Status, ca.CreatedByHSEId, u.FirstName || ' ' || u.LastName as CreatedByName, u.Email as CreatedByEmail, ca.CreatedAt, ca.ReportId
FROM CorrectiveActions ca
INNER JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE u.FirstName LIKE '%Yahya%' OR u.LastName LIKE '%Yahya%' OR u.Email LIKE '%yahya%'
ORDER BY ca.Id;

.print ""
.print "=== Action Count by Creator ==="
SELECT u.FirstName || ' ' || u.LastName as CreatorName, u.Email, COUNT(ca.Id) as ActionCount
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
GROUP BY ca.CreatedByHSEId, u.FirstName, u.LastName, u.Email
ORDER BY ActionCount DESC, CreatorName;
"@
            
            $queries | Out-File -FilePath $sqlFile -Encoding UTF8
            
            Write-Host "Executing SQLite queries..." -ForegroundColor Yellow
            & sqlite3.exe $DatabasePath ".read $sqlFile"
            
            Remove-Item $sqlFile -ErrorAction SilentlyContinue
        }
        
    } catch {
        Write-Host "❌ Alternative approach failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Suggestions:" -ForegroundColor Yellow
        Write-Host "1. Install SQLite3: choco install sqlite (if Chocolatey is installed)" -ForegroundColor Gray
        Write-Host "2. Download SQLite3 tools from https://www.sqlite.org/download.html" -ForegroundColor Gray
        Write-Host "3. Use Entity Framework in the HSEBackend project to query the database" -ForegroundColor Gray
        Write-Host "4. Use a database browser tool like DB Browser for SQLite" -ForegroundColor Gray
    }
}