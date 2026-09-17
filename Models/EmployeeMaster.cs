using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_EMP_MASTER")]
    public class EmployeeMaster
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Emp_Id")]
        public string EmpId { get; set; } = string.Empty;

        [Column("Emp_Nm")]
        public string EmpNm { get; set; } = string.Empty;

        [Column("Gender")]
        public string? Gender { get; set; }

        // NEW
        [Column("Vendor_Id")]
        public string? VendorId { get; set; }

        [Column("Vendor")]
        public string? Vendor { get; set; }

        [Column("Emp_Cat")]
        public string? EmpCat { get; set; }

        [Column("Dept_Nm")]
        public string? DeptNm { get; set; }

        [Column("Sub_Div")]
        public string? SubDiv { get; set; }

        [Column("Skill_Cat")]
        public string? SkillCat { get; set; }

        [Column("DOJ")]
        public DateTime? Doj { get; set; }

        [Column("DOL")]
        public DateTime? Dol { get; set; }

        [Column("Email_Id")]
        public string? EmailId { get; set; }

        [Column("Phone_No")]
        public string? PhoneNo { get; set; }

        [Column("UAN_No")]
        public string? UanNo { get; set; }

        [Column("PF_No")]
        public string? PfNo { get; set; }

        [Column("ESI_No")]
        public string? EsiNo { get; set; }

        [Column("PermAdd1")]
        public string? PermAdd1 { get; set; }

        [Column("PermAdd2")]
        public string? PermAdd2 { get; set; }

        [Column("PermStr")]
        public string? PermStr { get; set; }

        [Column("PermCity")]
        public string? PermCity { get; set; }

        [Column("PermPIN")]
        public string? PermPIN { get; set; }

        [Column("PermState")]
        public string? PermState { get; set; }

        [Column("PermCntry")]
        public string? PermCntry { get; set; }

        [Column("Marital")]
        public string? Marital { get; set; }

        [Column("DOB")]
        public DateTime? Dob { get; set; }

        [Column("AadharNo")]
        public string? AadharNo { get; set; }

        [Column("PAN_No")]
        public string? PanNo { get; set; }

        [Column("Qualify")]
        public string? Qualify { get; set; }

        [Column("Exp_Yrs")]
        public decimal? ExpYrs { get; set; }

        [Column("FatherNm")]
        public string? FatherNm { get; set; }

        [Column("Nation")]
        public string? Nation { get; set; }

        [Column("Status")]
        public string Status { get; set; } = "1";

        [Column("Crtd_On")]
        public DateTime CrtdOn { get; set; } = DateTime.Now;

        [Column("Crtd_By")]
        public string CrtdBy { get; set; } = "SYSTEM";

        [Column("Mdfd_On")]
        public DateTime? MdfdOn { get; set; }

        [Column("Mdfd_By")]
        public string? MdfdBy { get; set; }

        [Column("Dept_Code")]
        public string? DeptCode { get; set; }

        [Column("Sec_Code")]
        public string? SecCode { get; set; }

        [Column("Ctg_Code")]
        public string? CtgCode { get; set; }

        [Column("Grd_Code")]
        public string? GrdCode { get; set; }
    }
}