using YMI_PMT_PayrollManagement_API.DTOs.UserMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Helpers;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class UserMasterService : IUserMasterService
    {
        private readonly IUserMasterRepository _repository;

        public UserMasterService(IUserMasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UserMasterDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<UserMasterDTO?> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<List<string>> GetDepartmentsAsync()
        {
            return await _repository.GetDistinctDepartmentsAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(CreateUserMasterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return (false, "User Id is required", 0);

            if (await _repository.UserIdExistsAsync(dto.UserId))
                return (false, "User Id already exists", 0);

            if (string.IsNullOrWhiteSpace(dto.Password))
                return (false, "Password is required", 0);

            string hashedPassword = PasswordHasher.HashPassword(dto.Password);

            var entity = new UserMaster
            {
                UserId = dto.UserId.Trim(),
                UserName = dto.UserName.Trim(),
                DepartmentName = string.IsNullOrWhiteSpace(dto.DepartmentName) ? null : dto.DepartmentName.Trim(),
                UserType = string.IsNullOrWhiteSpace(dto.UserType) ? null : dto.UserType.Trim(),
                Password = hashedPassword,
                Status = dto.IsActive ? "1" : "0",
                CreatedOn = DateTime.Now,
                CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var id = await _repository.CreateAsync(entity);
            return (true, "User Saved Successfully", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateUserMasterDTO dto)
        {
            if (await _repository.UserIdExistsAsync(dto.UserId, id))
                return (false, "User Id already exists");

            bool updatePassword = !string.IsNullOrWhiteSpace(dto.Password);
            string? hashedPassword = null;

            if (updatePassword)
            {
                hashedPassword = PasswordHasher.HashPassword(dto.Password);
            }

            var entity = new UserMaster
            {
                Id = id,
                UserId = dto.UserId.Trim(),
                UserName = dto.UserName.Trim(),
                DepartmentName = string.IsNullOrWhiteSpace(dto.DepartmentName) ? null : dto.DepartmentName.Trim(),
                UserType = string.IsNullOrWhiteSpace(dto.UserType) ? null : dto.UserType.Trim(),
                Password = hashedPassword,
                Status = dto.IsActive ? "1" : "0",
                ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var result = await _repository.UpdateAsync(entity, updatePassword);
            return result ? (true, "User Updated Successfully") : (false, "User not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Deleted Successfully") : (false, "User not found");
        }

        // 👇 NEW
        public async Task<List<UserPrivilegeDTO>> GetPrivilegesAsync(int userId)
        {
            var list = await _repository.GetPrivilegesAsync(userId);
            return list.Select(p => new UserPrivilegeDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                MenuName = p.MenuName,
                CanView = p.CanView,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete
            }).ToList();
        }

        public async Task<(bool Success, string Message)> SavePrivilegesAsync(List<SaveUserPrivilegeDTO> privileges)
        {
            if (privileges == null || privileges.Count == 0)
                return (false, "No privileges provided");

            var userId = privileges.First().UserId;

            var entities = privileges
                .Where(p => p.CanView || p.CanEdit || p.CanDelete)
                .Select(p => new UserPrivilege
                {
                    UserId = p.UserId,
                    MenuName = p.MenuName,
                    CanView = p.CanView,
                    CanEdit = p.CanEdit,
                    CanDelete = p.CanDelete
                })
                .ToList();

            await _repository.SavePrivilegesAsync(userId, entities);
            return (true, "Privileges Saved");
        }

        private static UserMasterDTO MapToDto(UserMaster u) => new UserMasterDTO
        {
            Id = u.Id,
            UserId = u.UserId ?? string.Empty,
            UserName = u.UserName ?? string.Empty,
            DepartmentName = u.DepartmentName ?? string.Empty,
            UserType = u.UserType ?? string.Empty,
            Status = u.Status ?? string.Empty,
        };
    }
}