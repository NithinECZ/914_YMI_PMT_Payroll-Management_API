using YMI_PMT_PayrollManagement_API.DTOs.VendorMaster;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IVendorMasterService
    {
        Task<List<VendorMasterDTO>> GetAllAsync();
        Task<VendorMasterDTO?> GetByIdAsync(int id);
        Task<List<string>> GetVendorNamesAsync();
        Task<(bool Success, string Message, int Id)> CreateAsync(CreateVendorMasterDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, CreateVendorMasterDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);

    }
}