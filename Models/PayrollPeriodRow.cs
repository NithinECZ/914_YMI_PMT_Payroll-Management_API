using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    public class PayrollPeriodRow
    {
        [Column("From_Dt")]
        public DateTime FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime ToDate { get; set; }

        [Column("Emp_Count")]
        public int EmployeeCount { get; set; }

        [Column("Net_Total")]
        public decimal NetTotal { get; set; }
        [Column("Transaction_ID")]
        public string? TransactionId { get; set; }
    }
}