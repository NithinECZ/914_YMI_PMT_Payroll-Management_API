using YMI_PMT_PayrollManagement_API.DTOs.UserMaster;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IUserMasterService
    {
        Task<List<UserMasterDTO>> GetAllAsync();
        Task<UserMasterDTO?> GetByIdAsync(int id);
        Task<List<string>> GetDepartmentsAsync();
        Task<(bool Success, string Message, int Id)> CreateAsync(CreateUserMasterDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, CreateUserMasterDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);

        // 👇 NEW
        Task<List<UserPrivilegeDTO>> GetPrivilegesAsync(int userId);
        Task<(bool Success, string Message)> SavePrivilegesAsync(List<SaveUserPrivilegeDTO> privileges);
    }
}