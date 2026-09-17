using Microsoft.EntityFrameworkCore;
using System.Data;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class EmployeeMasterRepository : IEmployeeMasterRepository
    {
        private readonly AppDbContext _context;

        public EmployeeMasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeMaster>> GetAllAsync()
        {
            return await _context.EmployeeMasters
                .AsNoTracking()
                .OrderBy(e => e.EmpNm)
                .ToListAsync();
        }

        public async Task<EmployeeMaster?> GetByIdAsync(int id)
        {
            return await _context.EmployeeMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // Used for Genders / MaritalStatus SPs — single column result
        private async Task<List<string>> GetDistinctViaStoredProcAsync(string procName)
        {
            var result = new List<string>();
            var connection = _context.Database.GetDbConnection();

            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = procName;
                command.CommandType = CommandType.StoredProcedure;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!await reader.IsDBNullAsync(0))
                        result.Add(reader.GetString(0));
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }

        // NEW — used for Departments / EmpCategories / SubDivisions / SkillCategories SPs
        // These SPs return TWO columns: Code, Name — read both in a single fast round-trip
        private async Task<List<CodeNameDTO>> GetDistinctCodeNameViaStoredProcAsync(string procName)
        {
            var result = new List<CodeNameDTO>();
            var connection = _context.Database.GetDbConnection();

            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = procName;
                command.CommandType = CommandType.StoredProcedure;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new CodeNameDTO
                    {
                        Code = await reader.IsDBNullAsync(0) ? string.Empty : reader.GetString(0),
                        Name = await reader.IsDBNullAsync(1) ? string.Empty : reader.GetString(1)
                    });
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }

        public async Task<List<string>> GetDistinctGendersAsync()
            => await GetDistinctViaStoredProcAsync("sp_GetDistinctGenders");

        public async Task<List<CodeNameDTO>> GetDistinctEmpCategoriesAsync()
            => await GetDistinctCodeNameViaStoredProcAsync("sp_GetDistinctEmpCategories");

        public async Task<List<CodeNameDTO>> GetDistinctDepartmentsAsync()
            => await GetDistinctCodeNameViaStoredProcAsync("sp_GetDistinctDepartments");

        public async Task<List<CodeNameDTO>> GetDistinctSubDivisionsAsync()
            => await GetDistinctCodeNameViaStoredProcAsync("sp_GetDistinctSubDivisions");

        public async Task<List<CodeNameDTO>> GetDistinctSkillCategoriesAsync()
            => await GetDistinctCodeNameViaStoredProcAsync("sp_GetDistinctSkillCategories");

        public async Task<List<string>> GetDistinctMaritalStatusAsync()
            => await GetDistinctViaStoredProcAsync("sp_GetDistinctMaritalStatus");

        public async Task<List<VendorDropdownDTO>> GetVendorsAsync()
        {
            var result = new List<VendorDropdownDTO>();
            var connection = _context.Database.GetDbConnection();

            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "sp_GetVendorsForDropdown";
                command.CommandType = CommandType.StoredProcedure;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new VendorDropdownDTO
                    {
                        VendorId = reader["VendorId"]?.ToString() ?? string.Empty,
                        VendorName = reader["VendorName"]?.ToString() ?? string.Empty
                    });
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(EmployeeMaster employee)
        {
            var existing = await _context.EmployeeMasters.FirstOrDefaultAsync(e => e.Id == employee.Id);
            if (existing == null) return false;

            existing.VendorId = employee.VendorId;
            existing.Vendor = employee.Vendor;
            existing.Status = employee.Status ?? existing.Status;
            existing.MdfdOn = DateTime.Now;
            existing.MdfdBy = employee.MdfdBy ?? "SYSTEM";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.EmployeeMasters.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null) return false;

            _context.EmployeeMasters.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmpIdExistsAsync(string empId, int excludeId = 0)
        {
            return await _context.EmployeeMasters
                .AnyAsync(e => e.EmpId == empId && e.Id != excludeId);
        }
    }
}