using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    public class PayrollEmployee
    {
        public int? Id { get; set; }

        [Column("Emp_Id")]
        public string? EmployeeId { get; set; }

        [Column("Emp_Nm")]
        public string? EmployeeName { get; set; }

        [Column("Skill_Cat")]
        public string? SkillCategory { get; set; }

        // 👈 NEW: Vendor, pulled from YMT_EMP_MASTER via SP_GET_PAYROLL_EMPLOYEES
        [Column("Vendor")]
        public string? Vendor { get; set; }

        [Column("From_Dt")]
        public DateTime? FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime? ToDate { get; set; }

        [Column("P_Date")]
        public DateTime? AttendanceDate { get; set; }

        [Column("Sft_Strt")]
        public string? ShiftStart { get; set; }

        [Column("Sft_End")]
        public string? ShiftEnd { get; set; }

        [Column("Punch1")]
        public DateTime? Punch1 { get; set; }

        [Column("Punch2")]
        public DateTime? Punch2 { get; set; }

        [Column("WorkedMin")]
        public int? WorkedMinutes { get; set; }

        [Column("Day_Status")]
        public string? DayStatus { get; set; }

        [Column("Hol_Nm")]
        public string? HolidayName { get; set; }
    }
}