using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_SALARY_STRUCTURE_COMPONENT")]
    public class SalaryStructureComponent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("StrucId")]
        public int StructureId { get; set; }

        [Column("CompNm")]
        [MaxLength(150)]
        public string? ComponentName { get; set; }

        [Column("CompType")]
        [MaxLength(30)]
        public string? Type { get; set; }              // Earning / Deduction

        [Column("CalcMeth")]
        [MaxLength(50)]
        public string? CalculationMethod { get; set; } // Fixed Amount / Percentage / Per Hour

        [Column("AmtPct", TypeName = "decimal(18,4)")]
        public decimal? AmountOrPercentage { get; set; }

        [Column("CrtdOn")]
        public DateTime? CreatedOn { get; set; }

        [Column("CrtdBy")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [Column("MdfdOn")]
        public DateTime? ModifiedOn { get; set; }

        [Column("MdfdBy")]
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        [ForeignKey(nameof(StructureId))]
        public SalaryStructure? Structure { get; set; }
    }
}