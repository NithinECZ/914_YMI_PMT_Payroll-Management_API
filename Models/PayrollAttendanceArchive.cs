using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_PAYROLL_ATTENDANCE_ARCHIVE")]
    public class PayrollAttendanceArchive
    {
        [Column("Id")]
        public int Id { get; set; }

        [Column("Emp_Id")]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("Emp_Nm")]
        public string EmployeeName { get; set; } = string.Empty;

        [Column("Ctg_Code")]
        public string CategoryCode { get; set; } = string.Empty;

        [Column("Vendor")]
        public string? Vendor { get; set; }

        [Column("From_Dt")]
        public DateTime FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime ToDate { get; set; }

        [Column("Att_Dt")]
        public DateTime AttendanceDate { get; set; }

        [Column("Day_No")]
        public int? DayNumber { get; set; }

        [Column("Day_Status")]
        public string? DayStatus { get; set; }

        [Column("Hol_Nm")]
        public string? HolidayName { get; set; }

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

        [Column("Transaction_ID")]
        public string? TransactionId { get; set; }

        [Column("Version")]
        public string? Version { get; set; }

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; }
    }
}