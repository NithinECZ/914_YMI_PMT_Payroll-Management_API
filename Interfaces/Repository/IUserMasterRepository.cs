using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IUserMasterRepository
    {
        Task<List<UserMaster>> GetAllAsync();
        Task<UserMaster?> GetByIdAsync(int id);
        Task<List<string>> GetDistinctDepartmentsAsync();
        Task<int> CreateAsync(UserMaster user);
        Task<bool> UpdateAsync(UserMaster user, bool updatePassword);
        Task<bool> DeleteAsync(int id);
        Task<bool> UserIdExistsAsync(string userId, int excludeId = 0);

        // 👇 NEW
        Task<List<UserPrivilege>> GetPrivilegesAsync(int userId);
        Task SavePrivilegesAsync(int userId, List<UserPrivilege> privileges);
    }
}