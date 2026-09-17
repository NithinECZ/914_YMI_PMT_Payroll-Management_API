using YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IEmployeeMasterService
    {
        Task<List<EmployeeMasterDTO>> GetAllAsync();
        Task<EmployeeMasterDTO?> GetByIdAsync(int id);

        Task<List<string>> GetGendersAsync();
        Task<List<CodeNameDTO>> GetEmpCategoriesAsync();
        Task<List<CodeNameDTO>> GetDepartmentsAsync();
        Task<List<CodeNameDTO>> GetSubDivisionsAsync();
        Task<List<CodeNameDTO>> GetSkillCategoriesAsync();
        Task<List<string>> GetMaritalStatusAsync();
        Task<List<VendorDropdownDTO>> GetVendorsAsync();

        Task<(bool Success, string Message)> UpdateAsync(int id, CreateEmployeeMasterDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}