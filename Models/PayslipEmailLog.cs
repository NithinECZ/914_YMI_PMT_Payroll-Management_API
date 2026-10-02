using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_PAYSLIP_EMAIL_LOG")]
    public class PayslipEmailLog
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Emp_Id")]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("Emp_Nm")]
        public string? EmployeeName { get; set; }

        [Column("Skill_Cat")]
        public string? SkillCategory { get; set; }

        [Column("Ctg_Code")]
        public string CategoryCode { get; set; } = "CL";

        [Column("Vendor_Id")]
        public string? VendorId { get; set; }

        [Column("Vendor_Nm")]
        public string? VendorName { get; set; }

        [Column("Vendor_Email")]
        public string? VendorEmail { get; set; }

        [Column("From_Dt")]
        public DateTime? FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime? ToDate { get; set; }

        [Column("Mail_Sent_Status")]
        public string MailSentStatus { get; set; } = "FAILED";

        [Column("Mail_Sent_On")]
        public DateTime? MailSentOn { get; set; }

        [Column("Remarks")]
        public string? Remarks { get; set; }

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }
    }
}
