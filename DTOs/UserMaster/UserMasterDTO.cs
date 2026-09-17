namespace YMI_PMT_PayrollManagement_API.DTOs.UserMaster
{
    // Used when returning user data to frontend (never includes password)
    public class UserMasterDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Used when creating or updating a user
    public class CreateUserMasterDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Plain text password coming from frontend - hashing happens in the Service layer
        public string Password { get; set; } = string.Empty;

        public string ModifiedBy { get; set; } = "SYSTEM";
        public bool IsActive { get; set; } = true;
    }
}