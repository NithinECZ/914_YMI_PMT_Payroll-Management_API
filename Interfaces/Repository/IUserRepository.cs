using YMI_PMT_PayrollManagement_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IUserRepository
    {
        /// <summary>
        /// Validate user login and return user if successful
        /// Returns null if user not found, inactive, or password incorrect
        /// </summary>
        Task<UserMaster?> ValidateUserLoginAsync(string userId, string password);

        /// <summary>
        /// Check if user exists and is active
        /// </summary>
        Task<bool> UserExistsAsync(string userId);

        /// <summary>
        /// Get user by Id
        /// Returns null if user not found
        /// </summary>
        Task<UserMaster?> GetUserByIdAsync(int id);

        /// <summary>
        /// Get all active users
        /// </summary>
        Task<List<UserMaster>> GetAllUsersAsync();
    }
}