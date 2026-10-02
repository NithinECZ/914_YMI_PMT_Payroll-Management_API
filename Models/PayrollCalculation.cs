using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_PAYROLL_CALCULATION")]
    public class PayrollCalculation
    {
        [Column("Id")] public int Id { get; set; }
        [Column("Emp_Id")] public string EmployeeId { get; set; } = string.Empty;
        [Column("Vendor")] public string? Vendor { get; set; }
        [Column("Ctg_Code")] public string CategoryCode { get; set; } = string.Empty;
        [Column("From_Dt")] public DateTime? FromDate { get; set; }
        [Column("To_Dt")] public DateTime? ToDate { get; set; }
        [Column("Pay_Days_Full")] public decimal? PayDaysFull { get; set; }
        [Column("Pay_Days_Half")] public decimal? PayDaysHalf { get; set; }
        [Column("TWD")] public decimal? Twd { get; set; }
        [Column("OT_Hrs")] public decimal? OtHrs { get; set; }
        [Column("Attn_Bonus")] public decimal? AttnBonus { get; set; }
        [Column("Earned_Basic_DA")] public decimal? EarnedBasicDa { get; set; }
        [Column("Earned_HRA")] public decimal? EarnedHra { get; set; }
        [Column("OT_Amount")] public decimal? OtAmount { get; set; }
        [Column("Gross")] public decimal? Gross { get; set; }

        [Column("EPF")] public decimal? Epf { get; set; }
        [Column("ESI")] public decimal? Esi { get; set; }
        [Column("Bonus_Ded")] public decimal? BonusDed { get; set; }
        [Column("Ser_Char")] public decimal? SerChar { get; set; }
        [Column("LWF")] public decimal? Lwf { get; set; }
        [Column("Total_Ded")] public decimal? TotalDed { get; set; }

        [Column("Earned_Wages")] public decimal? EarnedWages { get; set; }
        [Column("OT_Earning")] public decimal? OtEarning { get; set; }
        [Column("Handling_Charge")] public decimal? HandlingCharge { get; set; }

        [Column("Hostel_Ded")] public decimal? HostelDed { get; set; }
        [Column("PF_Ded")] public decimal? PfDed { get; set; }
        [Column("ESI_Ded")] public decimal? EsiDed { get; set; }

        [Column("Net_Payable")] public decimal? NetPayable { get; set; }

        [Column("Status")] public string Status { get; set; } = "Pending";
        [Column("Approver_Status")] public string ApproverStatus { get; set; } = "Pending";

        // 👈 NEW: mirrors the checker's own status column, always starts as "Pending"
        [Column("Checker_Status")] public string CheckerStatus { get; set; } = "Pending";

        [Column("Checker_By")] public string? CheckerBy { get; set; }
        [Column("Checker_On")] public DateTime? CheckerOn { get; set; }

        [Column("Transaction_ID")] public string? TransactionId { get; set; }
        [Column("Version")] public string Version { get; set; } = "V1";
        [Column("Remarks")] public string? Remarks { get; set; }

        [Column("Crtd_On")] public DateTime CreatedOn { get; set; }
        [Column("Mdfd_On")] public DateTime? ModifiedOn { get; set; }
    }
}