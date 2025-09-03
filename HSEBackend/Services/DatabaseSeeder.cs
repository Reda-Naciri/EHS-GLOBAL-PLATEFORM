using HSEBackend.Models;
using HSEBackend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Services
{
    public class DatabaseSeeder
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            // Create roles if they don't exist
            string[] roles = { "Admin", "HSE", "Profil" };
            
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "reda.naciri@te.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Reda",
                    LastName = "Naciri",
                    Department = "HSE",
                    Zone = "Zone A",
                    Position = "HSE Manager",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("✅ Admin user created successfully!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("✅ Admin user already exists");
                
                // Update admin user with Company ID if missing
                if (string.IsNullOrEmpty(adminUser.CompanyId))
                {
                    adminUser.CompanyId = "TE55555";
                    var updateResult = await userManager.UpdateAsync(adminUser);
                    if (updateResult.Succeeded)
                    {
                        Console.WriteLine($"✅ Updated admin user Company ID to: {adminUser.CompanyId}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to update admin user Company ID");
                    }
                }
                else
                {
                    Console.WriteLine($"✅ Admin user Company ID is already set to: {adminUser.CompanyId}");
                }
            }

            // Seed departments if they don't exist
            await SeedDepartments(context);
            
            // Seed test pending users if they don't exist
            await SeedPendingUsers(context);
            
            // Seed test registration requests
            await SeedRegistrationRequests(context);
        }

        private static async Task SeedDepartments(AppDbContext context)
        {
            // Check for missing departments and add them
            var existingDepartments = await context.Departments
                .Select(d => d.Code)
                .ToListAsync();

            var allDepartments = new[]
            {
                new { Name = "Engineering", Code = "ENG", Description = "Engineering Department" },
                new { Name = "Production", Code = "PROD", Description = "Production Department" },
                new { Name = "Quality Assurance", Code = "QA", Description = "Quality Assurance Department" },
                new { Name = "Maintenance", Code = "MAINT", Description = "Maintenance Department" },
                new { Name = "Health, Safety & Environment", Code = "HSE", Description = "Health, Safety & Environment Department" },
                new { Name = "Operations", Code = "OPS", Description = "Operations Department" },
                new { Name = "Logistics", Code = "LOG", Description = "Logistics Department" },
                new { Name = "Finance", Code = "FIN", Description = "Finance Department" },
                new { Name = "Human Resources", Code = "HR", Description = "Human Resources Department" },
                new { Name = "Information Technology", Code = "IT", Description = "Information Technology Department" },
                new { Name = "Administration", Code = "ADMIN", Description = "Administration Department" },
                new { Name = "Sales", Code = "SALES", Description = "Sales Department" },
                new { Name = "Marketing", Code = "MKT", Description = "Marketing Department" }
            };

            var departmentsToAdd = new List<Department>();
            foreach (var dept in allDepartments)
            {
                if (!existingDepartments.Contains(dept.Code))
                {
                    departmentsToAdd.Add(new Department 
                    { 
                        Name = dept.Name, 
                        Code = dept.Code, 
                        Description = dept.Description, 
                        IsActive = true, 
                        CreatedAt = DateTime.UtcNow 
                    });
                }
            }

            if (departmentsToAdd.Any())
            {
                context.Departments.AddRange(departmentsToAdd);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ {departmentsToAdd.Count} new departments added successfully!");
            }
            else
            {
                Console.WriteLine("✅ All departments already exist");
            }
        }
        
        private static async Task SeedPendingUsers(AppDbContext context)
        {
            // PendingUsers table is for approved users only - no test data needed
            // Real registration requests go to RegistrationRequests table
            Console.WriteLine("✅ PendingUsers seeding skipped - only real approved users should be here");
        }
        
        private static async Task SeedRegistrationRequests(AppDbContext context)
        {
            // Check if there are any existing registration requests
            if (context.RegistrationRequests.Any())
            {
                Console.WriteLine("✅ Registration requests already exist");
                return;
            }

            var testRegistrationRequests = new List<RegistrationRequest>
            {
                new RegistrationRequest
                {
                    FullName = "John Smith",
                    CompanyId = "TE001234",
                    Email = "john.smith@te.com",
                    Department = "Engineering",
                    Position = "Software Engineer",
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddHours(-1)
                },
                new RegistrationRequest
                {
                    FullName = "Sarah Johnson",
                    CompanyId = "TE005678",
                    Email = "sarah.johnson@te.com", 
                    Department = "Production",
                    Position = "Production Operator",
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddHours(-3)
                },
                new RegistrationRequest
                {
                    FullName = "Mike Wilson",
                    CompanyId = "TE009876",
                    Email = "mike.wilson@te.com",
                    Department = "Quality Assurance",
                    Position = "Quality Inspector",
                    Status = "Approved",
                    SubmittedAt = DateTime.UtcNow.AddDays(-2)
                },
                new RegistrationRequest
                {
                    FullName = "Lisa Brown",
                    CompanyId = "TE012345",
                    Email = "lisa.brown@te.com",
                    Department = "Logistics",
                    Position = "Logistics Coordinator",
                    Status = "Rejected",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1)
                },
                new RegistrationRequest
                {
                    FullName = "David Lee",
                    CompanyId = "TE054321",
                    Email = "david.lee@te.com",
                    Department = "Engineering",
                    Position = "Mechanical Engineer",
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            };

            context.RegistrationRequests.AddRange(testRegistrationRequests);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ {testRegistrationRequests.Count} test registration requests added successfully!");
        }
    }
}