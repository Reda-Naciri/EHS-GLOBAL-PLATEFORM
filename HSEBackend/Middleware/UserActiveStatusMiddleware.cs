using Microsoft.AspNetCore.Identity;
using HSEBackend.Models;
using System.Security.Claims;

namespace HSEBackend.Middleware
{
    public class UserActiveStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActiveStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            // Skip for non-authenticated requests
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Skip for certain endpoints that don't need active status check
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (
                path.Contains("/auth/logout") ||
                path.Contains("/auth/validate") ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/api/reports") && context.Request.Method == "POST" // Allow external report submission
            ))
            {
                await _next(context);
                return;
            }

            // Get user ID from claims
            var userId = context.User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userManager.FindByIdAsync(userId);
                
                // Check if user exists and is active
                if (user == null || !user.IsActive)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var message = user == null 
                        ? "User not found" 
                        : "Your account has been deactivated. Please contact an administrator.";
                    
                    await context.Response.WriteAsync($"{{\"message\": \"{message}\"}}");
                    return;
                }

                // Update last activity if user is active
                if (user.LastActivityAt == null || 
                    user.LastActivityAt < DateTime.UtcNow.AddMinutes(-5)) // Only update every 5 minutes
                {
                    user.LastActivityAt = DateTime.UtcNow;
                    await userManager.UpdateAsync(user);
                }
            }

            await _next(context);
        }
    }

    public static class UserActiveStatusMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserActiveStatusCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserActiveStatusMiddleware>();
        }
    }
}