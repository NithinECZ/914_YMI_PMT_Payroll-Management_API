namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollPeriodSummaryDTO
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal NetTotal { get; set; }
        public string? TransactionId { get; set; }
    }
}