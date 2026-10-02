using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_ATTENDANCE")]
    public class YmtAttendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(20)] public string Emp_Id { get; set; } = null!;
        [MaxLength(100)] public string? Emp_Nm { get; set; }
        [Required, MaxLength(30)] public string P_Date { get; set; } = null!;

        public DateTime? Punch1 { get; set; }
        [MaxLength(20)] public string? P1_Date { get; set; }
        [MaxLength(20)] public string? P1_Time { get; set; }

        public DateTime? Punch2 { get; set; }
        [MaxLength(20)] public string? P2_Date { get; set; }
        [MaxLength(20)] public string? P2_Time { get; set; }

        [MaxLength(50)] public string? Sch_Shift { get; set; }
        [MaxLength(50)] public string? Wrk_Shift { get; set; }

        public decimal? Erly_In { get; set; }
        [MaxLength(20)] public string? ErIn_HM { get; set; }
        public decimal? Late_In { get; set; }
        [MaxLength(20)] public string? LtIn_HM { get; set; }
        public decimal? Erly_Out { get; set; }
        [MaxLength(20)] public string? ErOut_HM { get; set; }
        public decimal? Ovr_Stay { get; set; }
        [MaxLength(20)] public string? OvSty_HM { get; set; }

        [MaxLength(20)] public string? WkTm_HM { get; set; }
        [MaxLength(20)] public string? Fst_Half { get; set; }
        [MaxLength(20)] public string? Snd_Half { get; set; }
        [MaxLength(20)] public string? Sft_Strt { get; set; }
        [MaxLength(20)] public string? Sft_End { get; set; }

        public decimal? MinF_Hrs { get; set; }
        [MaxLength(20)] public string? MinF_HHM { get; set; }
        public decimal? MinH_Hrs { get; set; }
        [MaxLength(20)] public string? MinH_HHM { get; set; }

        public decimal? Net_Wrk { get; set; }
        [MaxLength(200)] public string? Summary { get; set; }
        [MaxLength(20)] public string? OutTm_HM { get; set; }
        public decimal? WSft_Hrs { get; set; }
        [MaxLength(20)] public string? WSft_HHM { get; set; }

        [MaxLength(1)] public string? Status { get; set; } = "1";
        public DateTime? Crtd_On { get; set; }
        [MaxLength(20)] public string? Crtd_By { get; set; }
        public DateTime? Mdfd_On { get; set; }
        [MaxLength(20)] public string? Mdfd_By { get; set; }
    }
}