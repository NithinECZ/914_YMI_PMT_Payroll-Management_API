namespace YMI_PMT_PayrollManagement_API.DTOs.UserMaster
{
    public class UserMasterDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;   // NEW
        public string Status { get; set; } = string.Empty;
    }

    public class CreateUserMasterDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;   // NEW
        public string Password { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public bool IsActive { get; set; } = true;
    }
}