using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    // Keyless model — result of SP_GET_PAYROLL_EMPLOYEES
    // One row per employee per day of the salary period
    public class PayrollEmployee
    {
        public int? Id { get; set; }

        [Column("Emp_Id")]
        public string? EmployeeId { get; set; }

        [Column("Emp_Nm")]
        public string? EmployeeName { get; set; }

        [Column("Skill_Cat")]
        public string? SkillCategory { get; set; }

        // ---- salary period (YMT_SALARY_FORMULA) ----
        [Column("TR_ID")]
        public string? TrId { get; set; }

        [Column("From_Dt")]
        public DateTime? FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime? ToDate { get; set; }

        // ---- one day of the period ----
        [Column("P_Date")]
        public DateTime? AttendanceDate { get; set; }

        // Shift assigned for this day
        [Column("Sft_Strt")]
        public string? ShiftStart { get; set; }

        [Column("Sft_End")]
        public string? ShiftEnd { get; set; }

        // Raw punch timestamps
        [Column("Punch1")]
        public DateTime? Punch1 { get; set; }

        [Column("Punch2")]
        public DateTime? Punch2 { get; set; }

        // Worked minutes = DATEDIFF(MINUTE, Punch1, Punch2)
        [Column("WorkedMin")]
        public int? WorkedMinutes { get; set; }

        // Day status: P, HP, OP, PL, A, PH, PM, WO, NULL
        [Column("Day_Status")]
        public string? DayStatus { get; set; }

        // Holiday name or special status
        [Column("Hol_Nm")]
        public string? HolidayName { get; set; }
    }
}