using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Data;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class MailConfigRepository : IMailConfigRepository
    {
        private readonly AppDbContext _context;

        public MailConfigRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MailConfig>> GetAllAsync()
        {
            return await _context.MailConfigs
                .AsNoTracking()
                .OrderBy(m => m.EmailServer)
                .ToListAsync();
        }

        public async Task<MailConfig?> GetByIdAsync(int id)
        {
            return await _context.MailConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<int> CreateAsync(MailConfig config)
        {
            config.CreatedOn = DateTime.Now;
            config.CreatedBy ??= "SYSTEM";
            config.Status ??= "1";

            _context.MailConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config.Id;
        }

        public async Task<bool> UpdateAsync(MailConfig config, bool updatePassword)
        {
            var existing = await _context.MailConfigs.FirstOrDefaultAsync(m => m.Id == config.Id);
            if (existing == null)
                return false;

            existing.EmailServer = config.EmailServer;
            existing.EmailPort = config.EmailPort;
            existing.EmailFrom = config.EmailFrom;
            existing.Status = config.Status ?? "1";
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = config.ModifiedBy ?? "SYSTEM";

            if (updatePassword && config.EmailPassword != null)
            {
                existing.EmailPassword = config.EmailPassword;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.MailConfigs.FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null)
                return false;

            _context.MailConfigs.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}