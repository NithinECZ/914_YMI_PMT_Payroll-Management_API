using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    public class EmpLeavingRow
    {
        [Column("Emp_Id")]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("LeavingDate")]
        public DateTime? LeavingDate { get; set; }
    }
}