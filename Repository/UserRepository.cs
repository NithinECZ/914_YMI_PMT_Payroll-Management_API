using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<UserMaster?> ValidateUserLoginAsync(string userId, string password)
        {
            try
            {
                var users = await _context.UserMasters
                    .FromSqlInterpolated($@"
        EXEC sp_ValidateUserLogin 
            @UserId = {userId.Trim()},
            @Password = {password}
    ")
                    .AsNoTracking()
                    .ToListAsync();

                var user = users.FirstOrDefault();

                System.Diagnostics.Debug.WriteLine($"[ValidateUserLoginAsync] UserId: {userId} | User Found: {user != null}");

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateUserLoginAsync] User not found or inactive");
                    return null;
                }

                if (user.Status == "INACTIVE" || user.Status == "0")
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateUserLoginAsync] User is inactive (Status = {user.Status})");
                    user.Status = "INACTIVE";
                    return user;
                }

                var verificationResult = PasswordHasher.VerifyPasswordWithMigration(password, user.Password);

                if (verificationResult == VerificationResult.Failed)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateUserLoginAsync] Password verification failed");
                    return null;
                }

                if (verificationResult == VerificationResult.Success)
                {
                    return user;
                }

                if (verificationResult == VerificationResult.LegacyMatch)
                {
                    string newHashedPassword = PasswordHasher.HashPassword(password);
                    await UpgradePasswordAsync(user.Id, newHashedPassword);
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateUserLoginAsync] Error: {ex.Message}");
                throw;
            }
        }

        private async Task UpgradePasswordAsync(int userId, string newHashedPassword)
        {
            try
            {
                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    user.Password = newHashedPassword;
                    user.ModifiedOn = DateTime.Now;
                    user.ModifiedBy = "SYSTEM";

                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine($"[UpgradePasswordAsync] Password upgraded for UserId: {user.UserId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradePasswordAsync] Warning: Could not upgrade password - {ex.Message}");
            }
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            try
            {
                return await _context.UserMasters
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == userId.Trim() && u.Status == "1");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserExistsAsync] Error: {ex.Message}");
                return false;
            }
        }

        public async Task<UserMaster?> GetUserByIdAsync(int id)
        {
            try
            {
                return await _context.UserMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetUserByIdAsync] Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<UserMaster>> GetAllUsersAsync()
        {
            try
            {
                return await _context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.Status == "1")
                    .OrderBy(u => u.UserName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllUsersAsync] Error: {ex.Message}");
                return new List<UserMaster>();
            }
        }
    }
}