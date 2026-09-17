using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IVendorMasterRepository
    {
        Task<List<VendorMaster>> GetAllAsync();
        Task<VendorMaster?> GetByIdAsync(int id);
        Task<int> CreateAsync(VendorMaster vendor);
        Task<bool> UpdateAsync(VendorMaster vendor);
        Task<bool> DeleteAsync(int id);
        Task<bool> VendorIdExistsAsync(string vendorId, int excludeId = 0);
        Task<List<string>> GetDistinctVendorNamesAsync();
    }
}