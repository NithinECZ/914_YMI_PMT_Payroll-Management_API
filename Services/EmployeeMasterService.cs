using YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class EmployeeMasterService : IEmployeeMasterService
    {
        private readonly IEmployeeMasterRepository _repository;

        public EmployeeMasterService(IEmployeeMasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EmployeeMasterDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<EmployeeMasterDTO?> GetByIdAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            return employee == null ? null : MapToDto(employee);
        }

        public async Task<List<string>> GetGendersAsync()
            => await _repository.GetDistinctGendersAsync();

        public async Task<List<CodeNameDTO>> GetEmpCategoriesAsync()
            => await _repository.GetDistinctEmpCategoriesAsync();

        public async Task<List<CodeNameDTO>> GetDepartmentsAsync()
            => await _repository.GetDistinctDepartmentsAsync();

        public async Task<List<CodeNameDTO>> GetSubDivisionsAsync()
            => await _repository.GetDistinctSubDivisionsAsync();

        public async Task<List<CodeNameDTO>> GetSkillCategoriesAsync()
            => await _repository.GetDistinctSkillCategoriesAsync();

        public async Task<List<string>> GetMaritalStatusAsync()
            => await _repository.GetDistinctMaritalStatusAsync();

        public async Task<List<VendorDropdownDTO>> GetVendorsAsync()
            => await _repository.GetVendorsAsync();

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateEmployeeMasterDTO dto)
        {
            // EmpId is intentionally NOT updated (locked on UI)
            var entity = new EmployeeMaster
            {
                Id = id,
                EmpNm = dto.EmpNm,
                Gender = dto.Gender,
                VendorId = dto.VendorId,
                Vendor = dto.Vendor,
                EmpCat = dto.EmpCat,
                DeptNm = dto.DeptNm,
                SubDiv = dto.SubDiv,
                SkillCat = dto.SkillCat,
                DeptCode = dto.DeptCode,
                SecCode = dto.SecCode,
                CtgCode = dto.CtgCode,
                GrdCode = dto.GrdCode,
                Doj = dto.Doj,
                Dol = dto.Dol,
                EmailId = dto.EmailId,
                PhoneNo = dto.PhoneNo,
                UanNo = dto.UanNo,
                PfNo = dto.PfNo,
                EsiNo = dto.EsiNo,
                PermAdd1 = dto.PermAdd1,
                PermAdd2 = dto.PermAdd2,
                PermStr = dto.PermStr,
                PermCity = dto.PermCity,
                PermPIN = dto.PermPIN,
                PermState = dto.PermState,
                PermCntry = dto.PermCntry,
                Marital = dto.Marital,
                Dob = dto.Dob,
                AadharNo = dto.AadharNo,
                PanNo = dto.PanNo,
                Qualify = dto.Qualify,
                ExpYrs = dto.ExpYrs,
                FatherNm = dto.FatherNm,
                Nation = dto.Nation,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "1" : dto.Status,
                MdfdBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy
            };

            var result = await _repository.UpdateAsync(entity);
            return result
                ? (true, "Employee Updated Successfully")
                : (false, "Employee not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result
                ? (true, "Deleted Successfully")
                : (false, "Employee not found");
        }

        private static EmployeeMasterDTO MapToDto(EmployeeMaster e) => new EmployeeMasterDTO
        {
            Id = e.Id,
            EmpId = e.EmpId ?? string.Empty,
            EmpNm = e.EmpNm ?? string.Empty,
            Gender = e.Gender ?? string.Empty,
            VendorId = e.VendorId ?? string.Empty,
            Vendor = e.Vendor ?? string.Empty,
            DeptCode = e.DeptCode ?? string.Empty,
            SecCode = e.SecCode ?? string.Empty,
            CtgCode = e.CtgCode ?? string.Empty,
            GrdCode = e.GrdCode ?? string.Empty,
            EmpCat = e.EmpCat ?? string.Empty,
            DeptNm = e.DeptNm ?? string.Empty,
            SubDiv = e.SubDiv ?? string.Empty,
            SkillCat = e.SkillCat ?? string.Empty,
            Doj = e.Doj,
            Dol = e.Dol,
            EmailId = e.EmailId ?? string.Empty,
            PhoneNo = e.PhoneNo ?? string.Empty,
            UanNo = e.UanNo ?? string.Empty,
            PfNo = e.PfNo ?? string.Empty,
            EsiNo = e.EsiNo ?? string.Empty,
            PermAdd1 = e.PermAdd1 ?? string.Empty,
            PermAdd2 = e.PermAdd2 ?? string.Empty,
            PermStr = e.PermStr ?? string.Empty,
            PermCity = e.PermCity ?? string.Empty,
            PermPIN = e.PermPIN ?? string.Empty,
            PermState = e.PermState ?? string.Empty,
            PermCntry = e.PermCntry ?? string.Empty,
            Marital = e.Marital ?? string.Empty,
            Dob = e.Dob,
            AadharNo = e.AadharNo ?? string.Empty,
            PanNo = e.PanNo ?? string.Empty,
            Qualify = e.Qualify ?? string.Empty,
            ExpYrs = e.ExpYrs,
            FatherNm = e.FatherNm ?? string.Empty,
            Nation = e.Nation ?? string.Empty,
            Status = e.Status ?? "1"
        };
    }
}