using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    // Maps to: [YMI_PMT].[dbo].[YMT_CALNDR]
    // NOTE: I don't have your existing Models/CalendarMaster.cs, so please diff this
    // against it and adjust if your real column/table names differ from what your
    // SELECT statement showed.
    [Table("YMT_CALNDR", Schema = "dbo")]
    public class CalendarMaster
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Mth_Cd")]
        public int Month { get; set; }

        [Column("Year_Cd")]
        public int Year { get; set; }

        [Column("Day_No")]
        public int Day { get; set; }

        [Column("Hol_Nm")]
        [MaxLength(200)]
        public string HolidayName { get; set; } = string.Empty;

        [Column("Hol_Typ")]
        [MaxLength(100)]
        public string HolidayType { get; set; } = string.Empty;

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; }

        [Column("Crtd_By")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }

        [Column("Mdfd_By")]
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }
    }
}