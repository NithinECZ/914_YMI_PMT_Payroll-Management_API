namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    // Whole payload for the Payroll screen
    public class PayrollDataDTO
    {
        public PayrollPeriodDTO? Period { get; set; }
        public List<PayrollEmployeeDTO> Employees { get; set; } = new();
    }

    // Active salary period (YMT_SALARY_FORMULA)
    public class PayrollPeriodDTO
    {
        public string TrId { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;   // yyyy-MM-dd
        public string ToDate { get; set; } = string.Empty;     // yyyy-MM-dd
    }

    public class PayrollEmployeeDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;

        public string ShiftStart { get; set; } = string.Empty;
        public string ShiftEnd { get; set; } = string.Empty;

        public List<PayrollAttendanceDayDTO> Attendance { get; set; } = new();
    }

    public class PayrollAttendanceDayDTO
    {
        public string Date { get; set; } = string.Empty;         // yyyy-MM-dd
        public string Status { get; set; } = string.Empty;       // P, HP, OP, PL, A, PH, PM, WO or ""
        public string ShiftStart { get; set; } = string.Empty;
        public string ShiftEnd { get; set; } = string.Empty;
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
    }

    // One row of provision data from the uploaded file
    public class PayrollProvisionUploadRowDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public Dictionary<string, decimal> Provisions { get; set; } = new();
    }
}