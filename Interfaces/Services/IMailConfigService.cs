using YMI_PMT_PayrollManagement_API.DTOs.MailConfig;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IMailConfigService
    {
        Task<List<MailConfigDTO>> GetAllAsync();
        Task<MailConfigDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message, int Id)> CreateAsync(CreateMailConfigDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, CreateMailConfigDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}