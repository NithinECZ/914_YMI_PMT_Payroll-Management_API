using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_EMAIL_QUEUE")]
    public class EmailQueueRecord
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Emp_Id")]
        [Required]
        [StringLength(50)]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("Emp_Nm")]
        [StringLength(200)]
        public string? EmployeeName { get; set; }

        [Column("Category")]
        [StringLength(20)]
        public string? Category { get; set; }

        [Column("Vendor")]
        [StringLength(100)]
        public string? Vendor { get; set; }

        [Column("Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        [Column("To_Email")]
        [Required]
        [StringLength(200)]
        public string ToEmail { get; set; } = string.Empty;

        [Column("Subject")]
        [StringLength(300)]
        public string? Subject { get; set; }

        [Column("Body")]
        public string? Body { get; set; }

        [Column("Status")]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";  // PENDING, SENT, FAILED

        [Column("Error_Msg")]
        public string? ErrorMessage { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Column("Sent_On")]
        public DateTime? SentOn { get; set; }

        [Column("Retry_Count")]
        public int RetryCount { get; set; } = 0;
    }
}