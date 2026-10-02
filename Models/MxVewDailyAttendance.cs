namespace YMI_PMT_PayrollManagement_API.Models
{
    // Result of dbo.usp_GetAttendanceForSync (source DB YMI_PMT)
    public class MxVewDailyAttendance
    {
        public string Emp_Id { get; set; } = null!;
        public string? Emp_Nm { get; set; }
        public string? P_Date { get; set; }

        public DateTime? Punch1 { get; set; }
        public string? P1_Date { get; set; }
        public string? P1_Time { get; set; }

        public DateTime? Punch2 { get; set; }
        public string? P2_Date { get; set; }
        public string? P2_Time { get; set; }

        public string? Sch_Shift { get; set; }
        public string? Wrk_Shift { get; set; }

        public decimal? Erly_In { get; set; }
        public string? ErIn_HM { get; set; }
        public decimal? Late_In { get; set; }
        public string? LtIn_HM { get; set; }
        public decimal? Erly_Out { get; set; }
        public string? ErOut_HM { get; set; }
        public decimal? Ovr_Stay { get; set; }
        public string? OvSty_HM { get; set; }

        public string? WkTm_HM { get; set; }
        public string? Fst_Half { get; set; }
        public string? Snd_Half { get; set; }
        public string? Sft_Strt { get; set; }
        public string? Sft_End { get; set; }

        public decimal? MinF_Hrs { get; set; }
        public string? MinF_HHM { get; set; }
        public decimal? MinH_Hrs { get; set; }
        public string? MinH_HHM { get; set; }

        public decimal? Net_Wrk { get; set; }
        public string? Summary { get; set; }
        public string? OutTm_HM { get; set; }
        public decimal? WSft_Hrs { get; set; }
        public string? WSft_HHM { get; set; }
    }
}