namespace HSEBackend.Dtos
{
    public class RegisterRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
