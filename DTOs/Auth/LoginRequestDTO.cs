namespace YMI_PMT_PayrollManagement_API.DTOs.Auth
{
    public class LoginRequestDTO
    {
        public string UserId { get; set; } = string.Empty;   // Changed from Username
        public string Password { get; set; } = string.Empty;
    }
}