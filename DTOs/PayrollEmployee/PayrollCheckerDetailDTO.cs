
namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollCheckerDetailDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? LeavingDate { get; set; }             // 👈 NEW

        public Dictionary<string, decimal> Provisions { get; set; } = new();
        public Dictionary<string, decimal> Earnings { get; set; } = new();
        public Dictionary<string, decimal> Deductions { get; set; } = new();

        public decimal? NetPayable { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public List<PayrollCheckerAttendanceDayDTO> AttendanceDetails { get; set; } = new();

        public int PresentDays { get; set; }
        public int HalfDays { get; set; }
        public int AbsentDays { get; set; }
        public int WeeklyOff { get; set; }
        public int Holidays { get; set; }
        public int ResignedDays { get; set; }                // 👈 NEW (RS days, not counted in payroll)
        public int TotalWorkingDays { get; set; }
        public decimal OtHours { get; set; }                 // from DB (Maker calculated)

        public decimal PaidDays { get; set; }
        public int Lop { get; set; }

        public decimal PayDaysFull { get; set; }
        public decimal PayDaysHalf { get; set; }
    }

    public class PayrollCheckerAttendanceDayDTO
    {
        public DateTime AttendanceDate { get; set; }
        public int Day { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShiftStart { get; set; } = string.Empty;
        public string ShiftEnd { get; set; } = string.Empty;
        public DateTime? Punch1 { get; set; }
        public DateTime? Punch2 { get; set; }
        public int? WorkedMinutes { get; set; }
        public string WorkedHours { get; set; } = string.Empty;   // 👈 NEW
        public decimal OtHours { get; set; }                      // 👈 NEW (per-day OT, same rule as Maker)
        public string HolidayName { get; set; } = string.Empty;
    }
}