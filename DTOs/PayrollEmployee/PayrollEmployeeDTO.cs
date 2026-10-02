namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollDataDTO
    {
        public PayrollPeriodDTO? Period { get; set; }
        public List<PayrollEmployeeDTO> Employees { get; set; } = new();
    }

    public class PayrollPeriodDTO
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
    }

    public class PayrollEmployeeDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public List<PayrollAttendanceDayDTO> Attendance { get; set; } = new();
        public Dictionary<string, decimal> Provisions { get; set; } = new();
        public Dictionary<string, decimal> Earnings { get; set; } = new();
        public Dictionary<string, decimal> Deductions { get; set; } = new();
        public decimal? NetPayable { get; set; }
        public string Status { get; set; } = "Open";
        public string CheckerStatus { get; set; } = string.Empty; // 👈 NEW: same value as Status (CHECKER-VERIFIED / CHECKER-REJECT / CHECKER-HOLD ...) -> UI pill
        public string Remarks { get; set; } = string.Empty;       // from YMT_PAYROLL_CALCULATION.Remarks
        public string? LeavingDate { get; set; }
    }

    public class PayrollAttendanceDayDTO
    {
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ShiftStart { get; set; } = string.Empty;
        public string ShiftEnd { get; set; } = string.Empty;
        public string Punch1 { get; set; } = string.Empty;
        public string Punch2 { get; set; } = string.Empty;
        public int? WorkedMinutes { get; set; }
        public string WorkedHours { get; set; } = string.Empty;
        public string HolidayName { get; set; } = string.Empty;
    }

    public class PayrollEmployeeUploadResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RowsProcessed { get; set; }
        public int RowsFailed { get; set; }
        public List<PayrollUploadIssueDTO> Issues { get; set; } = new();
        public string? TransactionId { get; set; }
        public string? Version { get; set; }
    }

    // one rejected / problematic Excel row
    public class PayrollUploadIssueDTO
    {
        public int RowNumber { get; set; }              // Excel row number (header = row 1)
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class PayrollProvisionUploadRowDTO
    {
        public int RowNumber { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public Dictionary<string, decimal> Provisions { get; set; } = new();
    }

    public class PayrollProvisionSaveRowDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal? ReferalBonus { get; set; }
        public decimal? SplAllow { get; set; }
        public decimal? Kaizen { get; set; }
        public decimal? GpaGmc { get; set; }
    }
}