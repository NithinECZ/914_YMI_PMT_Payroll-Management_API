namespace YMI_PMT_PayrollManagement_API.DTOs.Auth
{
    public class UserDataDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? UserType { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}