using YMI_PMT_PayrollManagement_API.DTOs.MailConfig;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class MailConfigService : IMailConfigService
    {
        private readonly IMailConfigRepository _repository;

        public MailConfigService(IMailConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<MailConfigDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<MailConfigDTO?> GetByIdAsync(int id)
        {
            var config = await _repository.GetByIdAsync(id);
            return config == null ? null : MapToDto(config);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(CreateMailConfigDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailServer))
                return (false, "Email Server is required", 0);

            if (string.IsNullOrWhiteSpace(dto.EmailPort))
                return (false, "Email Server Port is required", 0);

            if (!int.TryParse(dto.EmailPort, out _))
                return (false, "Email Server Port must be numeric", 0);

            if (string.IsNullOrWhiteSpace(dto.EmailFrom))
                return (false, "Email From is required", 0);

            if (string.IsNullOrWhiteSpace(dto.EmailPassword))
                return (false, "Email Password is required", 0);

            var entity = new MailConfig
            {
                EmailServer = dto.EmailServer.Trim(),
                EmailPort = dto.EmailPort.Trim(),
                EmailFrom = dto.EmailFrom.Trim(),
                EmailPassword = dto.EmailPassword.Trim(),
                Status = dto.IsActive ? "1" : "0",
                CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var id = await _repository.CreateAsync(entity);
            return (true, "Mail Configuration Saved Successfully", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateMailConfigDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailServer))
                return (false, "Email Server is required");

            if (string.IsNullOrWhiteSpace(dto.EmailPort))
                return (false, "Email Server Port is required");

            if (!int.TryParse(dto.EmailPort, out _))
                return (false, "Email Server Port must be numeric");

            if (string.IsNullOrWhiteSpace(dto.EmailFrom))
                return (false, "Email From is required");

            bool updatePassword = !string.IsNullOrWhiteSpace(dto.EmailPassword);

            var entity = new MailConfig
            {
                Id = id,
                EmailServer = dto.EmailServer.Trim(),
                EmailPort = dto.EmailPort.Trim(),
                EmailFrom = dto.EmailFrom.Trim(),
                EmailPassword = updatePassword ? dto.EmailPassword!.Trim() : null,
                Status = dto.IsActive ? "1" : "0",
                ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var result = await _repository.UpdateAsync(entity, updatePassword);
            return result ? (true, "Mail Configuration Updated Successfully") : (false, "Configuration not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Deleted Successfully") : (false, "Configuration not found");
        }

        private static MailConfigDTO MapToDto(MailConfig m) => new MailConfigDTO
        {
            Id = m.Id,
            EmailServer = m.EmailServer ?? string.Empty,
            EmailPort = m.EmailPort ?? string.Empty,
            EmailFrom = m.EmailFrom ?? string.Empty,
            Status = m.Status ?? string.Empty,
        };
    }
}