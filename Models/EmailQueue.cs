using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_EMAIL_QUEUE")]
    public class EmailQueue
    {
        [Column("Id")] public int Id { get; set; }
        [Column("EmployeeId")] public string EmployeeId { get; set; } = string.Empty;
        [Column("EmployeeName")] public string? EmployeeName { get; set; }
        [Column("CategoryCode")] public string? CategoryCode { get; set; }
        [Column("Vendor")] public string? Vendor { get; set; }
        [Column("Remarks")] public string? Remarks { get; set; }
        [Column("MailType")] public string MailType { get; set; } = "CHECKER_REJECTED";
        [Column("Status")] public string Status { get; set; } = "PENDING";
        [Column("CreatedOn")] public DateTime CreatedOn { get; set; } = DateTime.Now;
        [Column("ProcessedOn")] public DateTime? ProcessedOn { get; set; }
        [Column("ErrorMessage")] public string? ErrorMessage { get; set; }
    }
}