namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{

    public class PayrollCheckerStatusUpdateDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Category { get; set; } = "CL";
        public string Status { get; set; } = string.Empty;   // "rejected" | "hold" | "approved"
        public string? Remarks { get; set; }
        public string? CheckedBy { get; set; }
    }
}