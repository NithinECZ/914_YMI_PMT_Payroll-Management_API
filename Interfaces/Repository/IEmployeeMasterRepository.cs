using YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IEmployeeMasterRepository
    {
        Task<List<EmployeeMaster>> GetAllAsync();
        Task<EmployeeMaster?> GetByIdAsync(int id);

        Task<List<string>> GetDistinctGendersAsync();
        Task<List<CodeNameDTO>> GetDistinctEmpCategoriesAsync();
        Task<List<CodeNameDTO>> GetDistinctDepartmentsAsync();
        Task<List<CodeNameDTO>> GetDistinctSubDivisionsAsync();
        Task<List<CodeNameDTO>> GetDistinctSkillCategoriesAsync();
        Task<List<string>> GetDistinctMaritalStatusAsync();

        Task<List<VendorDropdownDTO>> GetVendorsAsync();

        Task<bool> UpdateAsync(EmployeeMaster employee);
        Task<bool> DeleteAsync(int id);
        Task<bool> EmpIdExistsAsync(string empId, int excludeId = 0);
    }
}