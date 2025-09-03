namespace HSEBackend.Models
{
    public class PendingUser
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string CompanyId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string Role { get; set; } = "Profil"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
