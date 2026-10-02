namespace YMI_PMT_PayrollManagement_API.DTOs.Payslip
{
    public class PayslipDataDTO
    {
        public List<PayslipEmployeeDTO> Employees { get; set; } = new();
    }

    public class PayslipEmployeeDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string? LeavingDate { get; set; }

        // Statutory & Identification
        public string? PfNo { get; set; }
        public string? EsiNo { get; set; }
        public string? PanNo { get; set; }
        public string? UanNo { get; set; }

        // Attendance & Working Days
        public decimal? WorkingDays { get; set; }
        public int? PeriodDays { get; set; }
        public decimal? PayDaysFull { get; set; }
        public decimal? PayDaysHalf { get; set; }
        public decimal? OtHrs { get; set; }

        // Financials
        public decimal? VariableTotal { get; set; }
        public decimal? RefBon { get; set; }
        public decimal? SplAllw { get; set; }
        public decimal? Kaizen { get; set; }
        public decimal? GpaGmc { get; set; }
        public decimal? Gross { get; set; }
        public decimal? TotalDed { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? NetPayable { get; set; }

        public Dictionary<string, decimal> Provisions { get; set; } = new();
        public Dictionary<string, decimal> Earnings { get; set; } = new();
        public Dictionary<string, decimal> Deductions { get; set; } = new();

        public string Status { get; set; } = "APPROVER-APPROVED";
        public string ApproverStatus { get; set; } = "APPROVER-APPROVED";
        public string CheckerStatus { get; set; } = "CHECKER-APPROVED";
        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
    }

    public class GeneratePayslipItemDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Category { get; set; } = "CL";
    }

    public class GeneratePayslipRequestDTO
    {
        public string Category { get; set; } = "CL";
        public string GeneratedBy { get; set; } = "System";
        public List<GeneratePayslipItemDTO> Employees { get; set; } = new();
    }

    public class GeneratePayslipResultDTO
    {
        public bool Success { get; set; }
        public int ProcessedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SendVendorEmailRequestDTO
    {
        public string Category { get; set; } = "CL";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> EmployeeIds { get; set; } = new();
        public List<PayslipEmployeeDTO>? Employees { get; set; }
        public string? SentBy { get; set; } = "System";
        public string FileFormat { get; set; } = "PDF"; // "PDF" / "DOC" or "EXCEL"
    }

    public class PayslipEmailStatusDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? SkillCategory { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorEmail { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string MailSentStatus { get; set; } = "FAILED";
        public DateTime? MailSentOn { get; set; }
        public string? Remarks { get; set; }
    }

    public class SendVendorEmailResultDTO
    {
        public bool Success { get; set; }
        public int TotalVendors { get; set; }
        public int SuccessfulVendors { get; set; }
        public int TotalEmployees { get; set; }
        public int SuccessfulEmployees { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
