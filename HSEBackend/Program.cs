using HSEBackend.Data;
using HSEBackend.Models;
using HSEBackend.Services;
using HSEBackend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SQL Server + Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 2. Configure JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)
        )
    };
});

builder.Services.AddAuthorization();

// 3. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            
            // Allow localhost on any port
            if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                return true;
                
            // Allow any IP address on ports 4200-4210 (common Angular dev ports)
            var uri = new Uri(origin);
            return uri.Port >= 4200 && uri.Port <= 4210;
        })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Register Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<HSEBackend.Interfaces.IReportService, HSEBackend.Services.ReportService>();
builder.Services.AddScoped<HSEBackend.Services.IAuthService, HSEBackend.Services.AuthService>();
builder.Services.AddScoped<HSEBackend.Services.IFileService, HSEBackend.Services.FileService>();
builder.Services.AddScoped<HSEBackend.Services.IHSEAccessControlService, HSEBackend.Services.HSEAccessControlService>();
builder.Services.AddScoped<HSEBackend.Services.ProgressCalculationService>();
builder.Services.AddScoped<HSEBackend.Services.IOverdueService, HSEBackend.Services.OverdueService>();

// Notification and Email Services
builder.Services.AddScoped<HSEBackend.Services.INotificationService, HSEBackend.Services.NotificationService>();
builder.Services.AddScoped<HSEBackend.Services.IEnhancedEmailService, HSEBackend.Services.EnhancedEmailService>();

// Register background service for automatic overdue updates
builder.Services.AddHostedService<HSEBackend.Services.OverdueBackgroundService>();

// Register background service for automatic email scheduling
builder.Services.AddSingleton<HSEBackend.Services.EmailSchedulingBackgroundService>();
builder.Services.AddHostedService<HSEBackend.Services.EmailSchedulingBackgroundService>(provider => 
    provider.GetService<HSEBackend.Services.EmailSchedulingBackgroundService>()!);

// Register background service for deadline and notification management
builder.Services.AddHostedService<HSEBackend.Services.DeadlineNotificationBackgroundService>();


var app = builder.Build();

// 5. Ensure Database Created and Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Ensure database is created
    context.Database.EnsureCreated();
    
    await HSEBackend.Services.DatabaseSeeder.SeedAsync(userManager, roleManager, context);
}

// Configure static files for uploaded content
app.UseStaticFiles(); // For wwwroot folder (avatars)

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// 6. Use CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseUserActiveStatusCheck();
app.UseAuthorization();
app.MapControllers();

// One-time status update for corrective actions after startup
_ = Task.Run(async () =>
{
    await Task.Delay(5000); // Wait 5 seconds for app to fully start
    
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var progressService = scope.ServiceProvider.GetRequiredService<ProgressCalculationService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🔄 Starting one-time corrective action status synchronization...");
        
        var correctiveActions = await context.CorrectiveActions
            .Include(ca => ca.SubActions)
            .Where(ca => ca.Status != "Aborted")
            .ToListAsync();

        var updateCount = 0;
        
        foreach (var correctiveAction in correctiveActions)
        {
            var oldStatus = correctiveAction.Status;
            var newStatus = progressService.CalculateParentStatus(correctiveAction.SubActions.ToList());

            if (oldStatus != newStatus)
            {
                correctiveAction.Status = newStatus;
                correctiveAction.IsCompleted = newStatus == "Completed";
                correctiveAction.UpdatedAt = DateTime.UtcNow;
                updateCount++;
                
                logger.LogInformation($"📝 Updated CorrectiveAction {correctiveAction.Id}: '{oldStatus}' → '{newStatus}'");
            }
        }

        if (updateCount > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation($"✅ Status synchronization completed: {updateCount}/{correctiveActions.Count} records updated");
        }
        else
        {
            logger.LogInformation($"✅ All corrective action statuses are already synchronized ({correctiveActions.Count} checked)");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during corrective action status synchronization");
    }
});

app.Run();
