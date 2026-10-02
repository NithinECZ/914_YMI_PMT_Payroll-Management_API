using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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

        // 👈 CHANGED: now passes @Stage too
        public async Task<List<PayrollEmployee>> GetPayrollEmployeesByStatusAsync(string category, string status, string? stage)
        {
            var stageParam = (object?)stage ?? DBNull.Value;
            return await _context.PayrollEmployees
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_EMPLOYEES_BY_STATUS] @Category = {category}, @Status = {status}, @Stage = {stageParam}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> SavePayrollProvisionsAsync(
            List<PayrollProvisionSaveRowDTO> rows,
            string category,
            DateTime? fromDate,
            DateTime? toDate,
            string? createdBy)
        {
            if (rows.Count == 0) return 0;

            var json = JsonSerializer.Serialize(rows.Select(r => new
            {
                r.EmployeeId,
                r.EmployeeName,
                r.ReferalBonus,
                r.SplAllow,
                r.Kaizen,
                r.GpaGmc
            }));

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                EXEC [dbo].[SP_SAVE_PAYROLL_PROVISIONS_BULK]
                    @Ctg_Code = {category},
                    @From_Dt  = {fromDate},
                    @To_Dt    = {toDate},
                    @Crtd_By  = {createdBy},
                    @RowsJson = {json}");

            return rows.Count;
        }

        public async Task<List<PayrollProvision>> GetPayrollProvisionsAsync(string category)
        {
            return await _context.PayrollProvisions
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_PROVISIONS] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(int Processed, int NoStructure)> CalculatePayrollAsync(string category, string? user = null)
        {
            var processed = new SqlParameter("@Processed", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var noStruct = new SqlParameter("@NoStructure", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[SP_CALCULATE_PAYROLL] @Category = @cat, @User = @usr, @Processed = @Processed OUTPUT, @NoStructure = @NoStructure OUTPUT",
                new SqlParameter("@cat", category),
                new SqlParameter("@usr", (object?)user ?? DBNull.Value),
                processed,
                noStruct);

            return ((int)processed.Value, (int)noStruct.Value);
        }

        public async Task<int> SubmitPayrollAsync(string category, string? user = null)
        {
            var submitted = new SqlParameter("@Submitted", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[SP_SUBMIT_PAYROLL] @Category = @cat, @User = @usr, @Submitted = @Submitted OUTPUT",
                new SqlParameter("@cat", category),
                new SqlParameter("@usr", (object?)user ?? DBNull.Value),
                submitted);

            return (int)submitted.Value;
        }

        public async Task<List<PayrollCalculation>> GetPayrollCalculationsAsync(string category)
        {
            return await _context.PayrollCalculations
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_CALCULATIONS] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollCheckerRow>> GetPayrollCheckerDataAsync(string category)
        {
            return await _context.Set<PayrollCheckerRow>()
                .FromSqlInterpolated($"EXEC dbo.SP_GET_PAYROLL_CHECKER_DATA @Category = {category}")
                .ToListAsync();
        }

        public async Task<List<PayrollEmployee>> GetPayrollCheckerAttendanceAsync(string employeeId, string category)
        {
            return await _context.Set<PayrollEmployee>()
                .FromSqlInterpolated($"EXEC dbo.SP_GET_PAYROLL_CHECKER_ATTENDANCE @EmployeeId = {employeeId}, @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> UpdateCheckerStatusAsync(
            string employeeId,
            string transactionId,
            string version,
            string category,
            string checkerStatus,
            string? remarks,
            string? checkerBy)
        {
            var rowsAffected = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[SP_UPDATE_PAYROLL_CHECKER_STATUS] " +
                "@Emp_Id=@empId, @Transaction_ID=@txnId, @Version=@ver, " +
                "@Checker_Status=@status, @Remarks=@remarks, @Checker_By=@checker, " +
                "@RowsAffected=@RowsAffected OUTPUT",
                new SqlParameter("@empId", employeeId),
                new SqlParameter("@txnId", transactionId),
                new SqlParameter("@ver", version),
                new SqlParameter("@status", checkerStatus),
                new SqlParameter("@remarks", (object?)remarks ?? DBNull.Value),
                new SqlParameter("@checker", (object?)checkerBy ?? DBNull.Value),
                rowsAffected);
            return (int)rowsAffected.Value > 0;
        }

        public async Task QueueRejectionEmailAsync(
            string employeeId,
            string? employeeName,
            string category,
            string? vendor,
            string? remarks)
        {
            _context.Set<EmailQueue>().Add(new EmailQueue
            {
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                CategoryCode = category,
                Vendor = vendor,
                Remarks = remarks,
                MailType = "CHECKER_REJECTED",
                Status = "PENDING",
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<int> SavePayrollAttendanceAsync(string category, string? user = null)
        {
            var inserted = new SqlParameter("@Inserted", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[SP_SAVE_PAYROLL_ATTENDANCE] @Category = @cat, @User = @usr",
                new SqlParameter("@cat", category),
                new SqlParameter("@usr", (object?)user ?? DBNull.Value));

            return inserted.Value != null ? (int)inserted.Value : 0;
        }

        public async Task<List<PayrollEmployee>> GetPayrollAnalysisAsync(
            string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to)
        {
            var f = from?.Date;
            var t = to?.Date;
            return await _context.PayrollEmployees
                .FromSqlInterpolated($@"EXEC [dbo].[SP_GET_PAYROLL_ANALYSIS]
            @Category = {category}, @Search = {search}, @Skill = {skill},
            @Vendor = {vendor}, @FromDt = {f}, @ToDt = {t}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollCalculation>> GetPayrollAnalysisCalculationsAsync(
            string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to)
        {
            var f = from?.Date;
            var t = to?.Date;
            return await _context.PayrollCalculations
                .FromSqlInterpolated($@"EXEC [dbo].[SP_GET_PAYROLL_ANALYSIS_CALC]
            @Category = {category}, @Search = {search}, @Skill = {skill},
            @Vendor = {vendor}, @FromDt = {f}, @ToDt = {t}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task QueueAdminResignEmailAsync(
            string employeeId, string? employeeName, string category, string? vendor, string? remarks)
        {
            _context.Set<EmailQueue>().Add(new EmailQueue
            {
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                CategoryCode = category,
                Vendor = vendor,
                Remarks = remarks,
                MailType = "ADMIN_RESIGN",
                Status = "PENDING",
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<EmpLeavingRow>> GetEmployeeLeavingDatesAsync()
        {
            return await _context.Set<EmpLeavingRow>()
                .FromSqlRaw(@"SELECT Emp_Id, DOL AS LeavingDate
                      FROM dbo.YMT_EMP_MASTER
                      WHERE DOL IS NOT NULL")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollPeriodRow>> GetPayrollPeriodsAsync(string category)
        {
            return await _context.Set<PayrollPeriodRow>()
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_PERIODS] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollEmployee>> GetPayrollHistoryAsync(string category, DateTime from, DateTime to)
        {
            var f = from.Date;
            var t = to.Date;
            return await _context.PayrollEmployees
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_HISTORY] @Category = {category}, @From = {f}, @To = {t}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollCalculation>> GetPayrollHistoryCalculationsAsync(string category, DateTime from, DateTime to)
        {
            var f = from.Date;
            var t = to.Date;
            return await _context.PayrollCalculations
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_HISTORY_CALC] @Category = {category}, @From = {f}, @To = {t}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollProvision>> GetPayrollHistoryProvisionsAsync(string category, DateTime from, DateTime to)
        {
            var f = from.Date;
            var t = to.Date;
            return await _context.PayrollProvisions
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_HISTORY_PROVISIONS] @Category = {category}, @From = {f}, @To = {t}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayrollEmployee>> GetPayrollLiveAttendanceAsync(string category)
        {
            return await _context.PayrollEmployees
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_LIVE_ATTENDANCE] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        // 👈 CHANGED: returns List<PayrollStageRow>
        public async Task<List<PayrollStageRow>> GetPayrollStatusSummaryAsync(string category)
        {
            return await _context.Set<PayrollStageRow>()
                .FromSqlInterpolated($"EXEC [dbo].[SP_GET_PAYROLL_STATUS_SUMMARY] @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }
    }
}