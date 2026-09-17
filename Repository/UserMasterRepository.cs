using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Data;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class UserMasterRepository : IUserMasterRepository
    {
        private readonly AppDbContext _context;

        public UserMasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserMaster>> GetAllAsync()
        {
            return await _context.UserMasters
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<UserMaster?> GetByIdAsync(int id)
        {
            return await _context.UserMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<string>> GetDistinctDepartmentsAsync()
        {
            return await _context.UserMasters
                .AsNoTracking()
                .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
                .Select(u => u.DepartmentName!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        public async Task<int> CreateAsync(UserMaster user)
        {
            if (string.IsNullOrEmpty(user.UserId))
                throw new ArgumentException("UserId is required");

            if (string.IsNullOrEmpty(user.UserName))
                throw new ArgumentException("UserName is required");

            if (string.IsNullOrEmpty(user.Status))
                user.Status = "1";

            user.CreatedOn = DateTime.Now;
            if (user.CreatedBy == null)
                user.CreatedBy = "SYSTEM";

            _context.UserMasters.Add(user);
            await _context.SaveChangesAsync();
            return user.Id;
        }

        public async Task<bool> UpdateAsync(UserMaster user, bool updatePassword)
        {
            var existing = await _context.UserMasters.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existing == null)
                return false;

            existing.UserName = user.UserName ?? existing.UserName;
            existing.DepartmentName = user.DepartmentName;
            existing.UserType = user.UserType;
            existing.Status = user.Status ?? "1";
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = user.ModifiedBy ?? "SYSTEM";

            if (updatePassword && user.Password != null)
            {
                existing.Password = user.Password;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.UserMasters.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
                return false;

            _context.UserMasters.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserIdExistsAsync(string userId, int excludeId = 0)
        {
            return await _context.UserMasters
                .AnyAsync(u => u.UserId == userId && u.Id != excludeId);
        }

        // 👇 NEW
        public async Task<List<UserPrivilege>> GetPrivilegesAsync(int userId)
        {
            return await _context.UserPrivileges
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task SavePrivilegesAsync(int userId, List<UserPrivilege> privileges)
        {
            var existing = await _context.UserPrivileges
                .Where(x => x.UserId == userId)
                .ToListAsync();

            _context.UserPrivileges.RemoveRange(existing);
            await _context.UserPrivileges.AddRangeAsync(privileges);
            await _context.SaveChangesAsync();
        }
    }
}