namespace YMI_PMT_PayrollManagement_API.DTOs.UserMaster
{
    public class UserPrivilegeDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class SaveUserPrivilegeDTO
    {
        public int UserId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}