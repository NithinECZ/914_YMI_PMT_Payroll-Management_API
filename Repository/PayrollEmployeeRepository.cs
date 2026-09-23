using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class PayrollEmployeeRepository : IPayrollEmployeeRepository
    {
        private readonly AppDbContext _context;

        public PayrollEmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PayrollEmployee>> GetPayrollEmployeesAsync(string category)
        {
            return await _context.PayrollEmployees
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_EMPLOYEES] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> SavePayrollProvisionsAsync(List<PayrollProvisionUploadRowDTO> rows, string category)
        {
            int successCount = 0;

            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        foreach (var prov in row.Provisions)
                        {
                            // SP_SAVE_PAYROLL_PROVISION handles insert/update logic with this signature:
                            // @EmployeeId NVARCHAR(50),
                            // @Category NVARCHAR(20),
                            // @ProvisionKey NVARCHAR(100),
                            // @Amount DECIMAL(18,2)
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $@"EXEC [dbo].[SP_SAVE_PAYROLL_PROVISION] 
                                    @EmployeeId = {row.EmployeeId}, 
                                    @Category = {category},
                                    @ProvisionKey = {prov.Key},
                                    @Amount = {prov.Value}"
                            );
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving provision for {row.EmployeeId}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SavePayrollProvisionsAsync: {ex.Message}");
                throw;
            }

            return successCount;
        }
    }
}