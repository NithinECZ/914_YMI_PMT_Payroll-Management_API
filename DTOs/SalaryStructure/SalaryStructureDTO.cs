using System.Collections.Generic;

namespace YMI_PMT_PayrollManagement_API.DTOs.SalaryStructure
{
    public class SalaryStructureDTO
    {
        public int Id { get; set; }
        public string StructureName { get; set; } = string.Empty;
        public string EmployeeCategory { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string? EffectiveFrom { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string? CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public string? ModifiedOn { get; set; }
        public List<SalaryStructureComponentDTO> Components { get; set; } = new();
    }

    public class SalaryStructureComponentDTO
    {
        public int Id { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = string.Empty;
        public decimal AmountOrPercentage { get; set; }
    }

    public class CreateSalaryStructureDTO
    {
        public string StructureName { get; set; } = string.Empty;
        public string EmployeeCategory { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string EffectiveFrom { get; set; } = string.Empty;
        public string Status { get; set; } = "1";
        public string ModifiedBy { get; set; } = "SYSTEM";
        public List<CreateSalaryStructureComponentDTO> Components { get; set; } = new();
    }

    public class CreateSalaryStructureComponentDTO
    {
        public string ComponentName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = string.Empty;
        public decimal AmountOrPercentage { get; set; }
    }

    public class ApiErrorDTO
    {
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
    }
}