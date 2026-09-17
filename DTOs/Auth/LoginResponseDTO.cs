namespace YMI_PMT_PayrollManagement_API.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDataDTO? User { get; set; }
    }
}