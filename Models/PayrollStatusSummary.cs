using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    public class PayrollStatusSummary
    {
        [Column("TotalEmployees")]
        public int TotalEmployees { get; set; }

        [Column("PendingCount")]
        public int PendingCount { get; set; }

        [Column("VerifyCount")]
        public int VerifyCount { get; set; }

        [Column("RejectCount")]
        public int RejectCount { get; set; }

        [Column("HoldCount")]
        public int HoldCount { get; set; }

        [Column("ApproveCount")]
        public int ApproveCount { get; set; }

        [Column("CheckerTouched")]
        public int CheckerTouched { get; set; }

        [Column("Stage")]
        public string? Stage { get; set; }

        [Column("From_Dt")]
        public DateTime? FromDate { get; set; }

        [Column("To_Dt")]
        public DateTime? ToDate { get; set; }
    }
}