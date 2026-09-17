using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_SALARY_STRUCTURE")]
    public class SalaryStructure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("StrucNm")]
        [MaxLength(150)]
        public string? StructureName { get; set; }

        [Column("EmpGrp")]
        [MaxLength(50)]
        public string? EmployeeCategory { get; set; }

        [Column("SkillCat")]
        [MaxLength(100)]
        public string? SkillCategory { get; set; }

        [Column("EffFrom")]
        public DateTime? EffectiveFrom { get; set; }

        [Column("Status")]
        [MaxLength(1)]
        public string? Status { get; set; }   // "1" = Active, "0" = Inactive

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

        public ICollection<SalaryStructureComponent> Components { get; set; }
            = new List<SalaryStructureComponent>();
    }
}