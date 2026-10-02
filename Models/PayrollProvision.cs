using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_PAYROLL_PROVISIONS")]
    public class PayrollProvision
    {
        [Column("Id")]
        public int Id { get; set; }

        [Column("Emp_Id")]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("Emp_Nm")]
        public string EmployeeName { get; set; } = string.Empty;

        [Column("Ctg_Code")]
        public string CategoryCode { get; set; } = string.Empty;

        // 👈 CHANGED: TrId property removed
        [Column("From_Dt")]
        public DateTime? FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime? ToDate { get; set; }

        [Column("Ref_Bon")]
        public decimal? ReferalBonus { get; set; }

        [Column("Spl_Allw")]
        public decimal? SplAllow { get; set; }

        [Column("Kaizen")]
        public decimal? Kaizen { get; set; }

        [Column("GPA_GMC")]
        public decimal? GpaGmc { get; set; }

        [Column("Attr1")]
        public string? Attr1 { get; set; }

        [Column("Attr2")]
        public string? Attr2 { get; set; }

        [Column("Attr3")]
        public string? Attr3 { get; set; }

        [Column("Attr4")]
        public string? Attr4 { get; set; }

        [Column("Attr5")]
        public string? Attr5 { get; set; }

        [Column("Attr6")]
        public string? Attr6 { get; set; }

        [Column("Status")]
        public string Status { get; set; } = "Open";

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }

        [Column("Crtd_On")]
        public DateTime? CreatedOn { get; set; }

        [Column("Mdfd_By")]
        public string? ModifiedBy { get; set; }

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }
    }
}