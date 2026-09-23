using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface ISalaryFormulaRepository
    {
        Task<List<SalaryFormula>> GetAllAsync();
        Task<SalaryFormula?> GetByIdAsync(int id);
        Task<int> CreateAsync(SalaryFormula entity);
        Task<bool> UpdateAsync(SalaryFormula entity);
        Task<bool> DeleteAsync(int id);
    }
}