using YMI_PMT_PayrollManagement_API.DTOs.SalaryFormula;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface ISalaryFormulaService
    {
        Task<List<SalaryFormulaDTO>> GetAllAsync();
        Task<SalaryFormulaDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message, int Id)> CreateAsync(CreateSalaryFormulaDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, CreateSalaryFormulaDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}