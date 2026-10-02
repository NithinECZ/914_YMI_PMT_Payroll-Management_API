using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Keyless]   // 👈 ADD THIS — REQUIRED for SP result mapping
    public class PayrollStageRow
    {
        [Column("Stage")] public string Stage { get; set; } = string.Empty;
        [Column("Total")] public int Total { get; set; }
        [Column("VerifyCount")] public int VerifyCount { get; set; }
        [Column("HoldCount")] public int HoldCount { get; set; }
        [Column("RejectCount")] public int RejectCount { get; set; }
        [Column("ApproveCount")] public int ApproveCount { get; set; }
        [Column("From_Dt")] public DateTime? FromDate { get; set; }
        [Column("To_Dt")] public DateTime? ToDate { get; set; }
    }
}