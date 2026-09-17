namespace YMI_PMT_PayrollManagement_API.DTOs.MailConfig
{
    // Used when returning config list/detail to frontend (password never sent back)
    public class MailConfigDTO
    {
        public int Id { get; set; }
        public string EmailServer { get; set; } = string.Empty;
        public string EmailPort { get; set; } = string.Empty;
        public string EmailFrom { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Used when creating or updating a mail configuration
    public class CreateMailConfigDTO
    {
        public string EmailServer { get; set; } = string.Empty;
        public string EmailPort { get; set; } = string.Empty;
        public string EmailFrom { get; set; } = string.Empty;

        // Leave blank on update to keep the existing password
        public string? EmailPassword { get; set; }

        public string ModifiedBy { get; set; } = "SYSTEM";
        public bool IsActive { get; set; } = true;
    }
}