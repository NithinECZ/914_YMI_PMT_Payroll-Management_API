using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IMailConfigRepository
    {
        Task<List<MailConfig>> GetAllAsync();
        Task<MailConfig?> GetByIdAsync(int id);
        Task<int> CreateAsync(MailConfig config);
        Task<bool> UpdateAsync(MailConfig config, bool updatePassword);
        Task<bool> DeleteAsync(int id);
    }
}