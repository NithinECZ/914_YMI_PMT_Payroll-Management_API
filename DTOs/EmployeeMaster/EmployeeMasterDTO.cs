namespace YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster
{
    public class EmployeeMasterDTO
    {
        public int Id { get; set; }
        public string EmpId { get; set; } = string.Empty;
        public string EmpNm { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        public string VendorId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;

        public string DeptCode { get; set; } = string.Empty;
        public string SecCode { get; set; } = string.Empty;
        public string CtgCode { get; set; } = string.Empty;
        public string GrdCode { get; set; } = string.Empty;

        public string EmpCat { get; set; } = string.Empty;
        public string DeptNm { get; set; } = string.Empty;
        public string SubDiv { get; set; } = string.Empty;
        public string SkillCat { get; set; } = string.Empty;

        public DateTime? Doj { get; set; }
        public DateTime? Dol { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string UanNo { get; set; } = string.Empty;
        public string PfNo { get; set; } = string.Empty;
        public string EsiNo { get; set; } = string.Empty;
        public string PermAdd1 { get; set; } = string.Empty;
        public string PermAdd2 { get; set; } = string.Empty;
        public string PermStr { get; set; } = string.Empty;
        public string PermCity { get; set; } = string.Empty;
        public string PermPIN { get; set; } = string.Empty;
        public string PermState { get; set; } = string.Empty;
        public string PermCntry { get; set; } = string.Empty;
        public string Marital { get; set; } = string.Empty;
        public DateTime? Dob { get; set; }
        public string AadharNo { get; set; } = string.Empty;
        public string PanNo { get; set; } = string.Empty;
        public string Qualify { get; set; } = string.Empty;
        public decimal? ExpYrs { get; set; }
        public string FatherNm { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public string Status { get; set; } = "1";
    }

    public class CreateEmployeeMasterDTO
    {
        public string EmpId { get; set; } = string.Empty;
        public string EmpNm { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        public string VendorId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;

        public string DeptCode { get; set; } = string.Empty;
        public string SecCode { get; set; } = string.Empty;
        public string CtgCode { get; set; } = string.Empty;
        public string GrdCode { get; set; } = string.Empty;

        public string EmpCat { get; set; } = string.Empty;
        public string DeptNm { get; set; } = string.Empty;
        public string SubDiv { get; set; } = string.Empty;
        public string SkillCat { get; set; } = string.Empty;

        public DateTime? Doj { get; set; }
        public DateTime? Dol { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string UanNo { get; set; } = string.Empty;
        public string PfNo { get; set; } = string.Empty;
        public string EsiNo { get; set; } = string.Empty;
        public string PermAdd1 { get; set; } = string.Empty;
        public string PermAdd2 { get; set; } = string.Empty;
        public string PermStr { get; set; } = string.Empty;
        public string PermCity { get; set; } = string.Empty;
        public string PermPIN { get; set; } = string.Empty;
        public string PermState { get; set; } = string.Empty;
        public string PermCntry { get; set; } = string.Empty;
        public string Marital { get; set; } = string.Empty;
        public DateTime? Dob { get; set; }
        public string AadharNo { get; set; } = string.Empty;
        public string PanNo { get; set; } = string.Empty;
        public string Qualify { get; set; } = string.Empty;
        public decimal? ExpYrs { get; set; }
        public string FatherNm { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public string Status { get; set; } = "1";
    }

    public class VendorDropdownDTO
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
    }

    // NEW — used for Departments / EmpCategories / SubDivisions / SkillCategories dropdowns
    // (these SPs return two columns: Code, Name)
    public class CodeNameDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}