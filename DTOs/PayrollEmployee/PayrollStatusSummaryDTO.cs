using System.Collections.Generic;

namespace YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee
{
    public class PayrollStatusSummaryDTO
    {
        // Overall (all active stages combined)
        public int TotalEmployees { get; set; }
        public int PendingCount { get; set; }
        public int VerifyCount { get; set; }
        public int RejectCount { get; set; }
        public int HoldCount { get; set; }
        public int ApproveCount { get; set; }

        // Primary stage (highest workflow stage present) — for backward-compat
        public string Stage { get; set; } = "PENDING";

        public string? FromDate { get; set; }
        public string? ToDate { get; set; }

        // List of active stages (stacked vertically on UI)
        public List<PayrollStageSummaryDTO> Stages { get; set; } = new();
    }

    public class PayrollStageSummaryDTO
    {
        public string Stage { get; set; } = string.Empty;  // PENDING | VERIFYING | APPROVE_PENDING | APPROVING
        public int Total { get; set; }
        public int Verify { get; set; }
        public int Hold { get; set; }
        public int Reject { get; set; }
        public int Approve { get; set; }
    }
}