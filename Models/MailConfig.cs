using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_MAIL_CONFIG")]
    public class MailConfig
    {
        [Key]
        public int Id { get; set; }

        [Column("Mail_Srv")]
        public string? EmailServer { get; set; }

        [Column("Mail_Prt")]
        public string? EmailPort { get; set; }

        [Column("Mail_Frm")]
        public string? EmailFrom { get; set; }

        [Column("Mail_Pwd")]
        public string? EmailPassword { get; set; }

        [Column("Status")]
        public string? Status { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; }

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }

        [Column("Mdfd_By")]
        public string? ModifiedBy { get; set; }
    }
}