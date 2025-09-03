using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

// Simple models for querying
public class ApplicationUser
{
    public string Id { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class CorrectiveAction
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public System.DateTime DueDate { get; set; }
    public string Status { get; set; } = "";
    public string? CreatedByHSEId { get; set; }
    public virtual ApplicationUser? CreatedByHSE { get; set; }
    public System.DateTime CreatedAt { get; set; }
    public int? ReportId { get; set; }
}

public class QueryDbContext : DbContext
{
    public DbSet<ApplicationUser> AspNetUsers { get; set; }
    public DbSet<CorrectiveAction> CorrectiveActions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=../HSEBackend/HSE_DB.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure ApplicationUser as Identity table
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasKey(e => e.Id);
        });

        // Configure CorrectiveAction
        modelBuilder.Entity<CorrectiveAction>(entity =>
        {
            entity.ToTable("CorrectiveActions");
            entity.HasKey(e => e.Id);
            entity.HasOne(ca => ca.CreatedByHSE)
                  .WithMany()
                  .HasForeignKey(ca => ca.CreatedByHSEId);
        });
    }
}

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        try
        {
            using var context = new QueryDbContext();
            
            Console.WriteLine("=== HSE Database Investigation ===");
            Console.WriteLine($"Database file: ../HSEBackend/HSE_DB.db");
            Console.WriteLine();

            // First, list all users to find Yahya
            Console.WriteLine("=== All Users in Database ===");
            var allUsers = await context.AspNetUsers
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.CompanyId })
                .ToListAsync();
                
            foreach (var user in allUsers)
            {
                Console.WriteLine($"ID: {user.Id}, Name: {user.FirstName} {user.LastName}, Email: {user.Email}, CompanyId: {user.CompanyId}");
            }
            
            Console.WriteLine();
            
            // Find Yahya by name
            var yahyaUsers = await context.AspNetUsers
                .Where(u => u.FirstName.Contains("Yahya") || u.LastName.Contains("Yahya") || u.Email.Contains("yahya"))
                .ToListAsync();
                
            Console.WriteLine("=== Users matching 'Yahya' ===");
            foreach (var user in yahyaUsers)
            {
                Console.WriteLine($"ID: {user.Id}, Name: {user.FirstName} {user.LastName}, Email: {user.Email}");
            }
            
            Console.WriteLine();
            
            // List all CorrectiveActions
            Console.WriteLine("=== All Corrective Actions ===");
            var allActions = await context.CorrectiveActions
                .Include(ca => ca.CreatedByHSE)
                .OrderBy(ca => ca.Id)
                .ToListAsync();
                
            Console.WriteLine($"Total CorrectiveActions found: {allActions.Count}");
            Console.WriteLine();
            
            foreach (var action in allActions)
            {
                var createdByName = action.CreatedByHSE != null 
                    ? $"{action.CreatedByHSE.FirstName} {action.CreatedByHSE.LastName}" 
                    : "Unknown";
                    
                Console.WriteLine($"ID: {action.Id}");
                Console.WriteLine($"  Title: {action.Title}");
                Console.WriteLine($"  Status: {action.Status}");
                Console.WriteLine($"  CreatedByHSEId: {action.CreatedByHSEId}");
                Console.WriteLine($"  CreatedBy: {createdByName}");
                Console.WriteLine($"  CreatedAt: {action.CreatedAt}");
                Console.WriteLine($"  ReportId: {action.ReportId}");
                Console.WriteLine();
            }
            
            // Check specific IDs mentioned in the logs [42, 43, 45, 46]
            Console.WriteLine("=== Specific Actions from Logs [42, 43, 45, 46] ===");
            var specificIds = new[] { 42, 43, 45, 46 };
            var specificActions = await context.CorrectiveActions
                .Include(ca => ca.CreatedByHSE)
                .Where(ca => specificIds.Contains(ca.Id))
                .OrderBy(ca => ca.Id)
                .ToListAsync();
                
            foreach (var action in specificActions)
            {
                var createdByName = action.CreatedByHSE != null 
                    ? $"{action.CreatedByHSE.FirstName} {action.CreatedByHSE.LastName}" 
                    : "Unknown";
                    
                Console.WriteLine($"ID: {action.Id} - Created by {createdByName} (HSE ID: {action.CreatedByHSEId})");
            }
            
            // If we found Yahya, show his actions
            if (yahyaUsers.Any())
            {
                Console.WriteLine();
                Console.WriteLine("=== Actions Created by Yahya ===");
                
                var yahyaIds = yahyaUsers.Select(u => u.Id).ToList();
                var yahyaActions = await context.CorrectiveActions
                    .Include(ca => ca.CreatedByHSE)
                    .Where(ca => ca.CreatedByHSEId != null && yahyaIds.Contains(ca.CreatedByHSEId))
                    .OrderBy(ca => ca.Id)
                    .ToListAsync();
                    
                Console.WriteLine($"Actions created by Yahya: {yahyaActions.Count}");
                foreach (var action in yahyaActions)
                {
                    Console.WriteLine($"  ID: {action.Id} - {action.Title} (Status: {action.Status})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}