namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    using YMI_PMT_PayrollManagement_API.DTOs.SalaryStructure;

    public interface ISalaryStructureService
    {
        Task<List<SalaryStructureDTO>> GetAllAsync();
        Task<SalaryStructureDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message, string? Field, int Id)> CreateAsync(CreateSalaryStructureDTO dto);
        Task<(bool Success, string Message, string? Field)> UpdateAsync(int id, CreateSalaryStructureDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}