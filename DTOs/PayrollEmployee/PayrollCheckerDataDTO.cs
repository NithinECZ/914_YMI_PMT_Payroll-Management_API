namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollCheckerDataDTO
    {
        // 👈 NEW: Settings period (From / To) even when grid is empty
        public PayrollPeriodDTO? Period { get; set; }

        public List<PayrollCheckerEmployeeDTO> Employees { get; set; } = new();
    }

    public class PayrollCheckerEmployeeDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // 👈 NEW: Date of Leaving (yyyy-MM-dd) -> RESIGNED badge
        public string? LeavingDate { get; set; }

        public Dictionary<string, decimal> Provisions { get; set; } = new();
        public Dictionary<string, decimal> Earnings { get; set; } = new();
        public Dictionary<string, decimal> Deductions { get; set; } = new();

        public decimal? NetPayable { get; set; }
        public string Status { get; set; } = "pending";

        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}