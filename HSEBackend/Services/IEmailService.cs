using System.Threading.Tasks;

namespace HSEBackend.Services
{
    public interface IEmailService
    {
        Task SendGenericEmail(string toEmail, string subject, string body);
        Task SendUserCreationEmailAsync(string email, string firstName, string lastName, string role, string? password = null, string? companyId = null, string? department = null, string? position = null);
        Task SendRoleChangeEmailAsync(string email, string firstName, string lastName, string oldRole, string newRole, string? newPassword = null, string? companyId = null);
    }
}
