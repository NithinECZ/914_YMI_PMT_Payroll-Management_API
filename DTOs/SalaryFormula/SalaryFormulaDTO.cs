namespace YMI_PMT_PayrollManagement_API.DTOs.SalaryFormula
{
    public class SalaryFormulaDTO
    {
        public int Id { get; set; }
        public string TrId { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

    public class CreateSalaryFormulaDTO
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string ModifiedBy { get; set; } = "SYSTEM";
    }
}