using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface ISalaryStructureRepository
    {
        Task<List<SalaryStructure>> GetAllAsync();
        Task<SalaryStructure?> GetByIdAsync(int id);
        Task<int> CreateAsync(SalaryStructure entity, List<SalaryStructureComponent> components);
        Task<bool> UpdateAsync(SalaryStructure entity, List<SalaryStructureComponent> components);
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Case-insensitive, trim-insensitive duplicate check.
        /// Pass the row's own id as excludeId while updating.
        /// </summary>
        Task<bool> StructureNameExistsAsync(string structureName, int excludeId = 0);

        Task<bool> ExistsAsync(int id);
    }
}