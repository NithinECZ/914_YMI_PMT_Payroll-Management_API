namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollResignRequestDTO
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string Category { get; set; } = "CL";
        public string? Vendor { get; set; }
        public string? Remarks { get; set; }
        public string? ResignDate { get; set; }   // yyyy-MM-dd (clicked date)
    }
}