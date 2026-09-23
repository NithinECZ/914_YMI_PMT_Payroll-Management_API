using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_SALARY_FORMULA")]
    public class SalaryFormula
    {
        [Key]
        public int Id { get; set; }

        [Column("TR_ID")]
        public string? TrId { get; set; } = string.Empty;

        [Column("From_Dt")]
        public DateTime FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime ToDate { get; set; }

        [Column("Status")]
        public string? Status { get; set; } = "1";

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