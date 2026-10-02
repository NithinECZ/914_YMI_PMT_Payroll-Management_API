using System.ComponentModel.DataAnnotations.Schema;
namespace YMI_PMT_PayrollManagement_API.Models
{
    public class PayrollCheckerRow
    {
        [Column("Id")] public int Id { get; set; }
        [Column("EmployeeId")] public string EmployeeId { get; set; } = string.Empty;
        [Column("EmployeeName")] public string EmployeeName { get; set; } = string.Empty;
        [Column("CategoryCode")] public string CategoryCode { get; set; } = string.Empty;
        [Column("SkillCategory")] public string? SkillCategory { get; set; }
        [Column("Vendor")] public string? Vendor { get; set; }
        [Column("FromDate")] public DateTime? FromDate { get; set; }
        [Column("ToDate")] public DateTime? ToDate { get; set; }

        [Column("RefBon")] public decimal? RefBon { get; set; }
        [Column("SplAllw")] public decimal? SplAllw { get; set; }
        [Column("Kaizen")] public decimal? Kaizen { get; set; }
        [Column("GpaGmc")] public decimal? GpaGmc { get; set; }

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

        [Column("NetPayable")] public decimal? NetPayable { get; set; }
        [Column("Status")] public string? Status { get; set; }
        [Column("CheckerStatus")] public string? CheckerStatus { get; set; }   // 👈 NEW
        [Column("ApproverStatus")] public string? ApproverStatus { get; set; }
        [Column("TransactionId")] public string? TransactionId { get; set; }
        [Column("Version")] public string? Version { get; set; }
        [Column("LeavingDate")] public DateTime? LeavingDate { get; set; }
    }
}