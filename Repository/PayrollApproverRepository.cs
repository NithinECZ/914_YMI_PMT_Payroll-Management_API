using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class PayrollApproverRepository : IPayrollApproverRepository
    {
        private readonly AppDbContext _context;

        public PayrollApproverRepository(AppDbContext context)
        {
            _context = context;
        }

        // SP_GET_PAYROLL_APPROVER_DATA returns ONLY the latest version per employee
        public async Task<List<PayrollCheckerRow>> GetPayrollApproverDataAsync(string category)
        {
            return await _context.Set<PayrollCheckerRow>()
                .FromSqlInterpolated($"EXEC dbo.SP_GET_PAYROLL_APPROVER_DATA @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        // Same attendance SP the Checker uses (saved archive first, live fallback, RS / PL / PH / PM / WO)
        public async Task<List<PayrollEmployee>> GetPayrollApproverAttendanceAsync(string employeeId, string category)
        {
            return await _context.Set<PayrollEmployee>()
                .FromSqlInterpolated($"EXEC dbo.SP_GET_PAYROLL_CHECKER_ATTENDANCE @EmployeeId = {employeeId}, @Category = {category}")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> UpdateApproverStatusAsync(
            string employeeId,
            string transactionId,
            string version,
            string category,
            string approverStatus,
            string? remarks,
            string? approverBy)
        {
            var rowsAffected = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[SP_UPDATE_PAYROLL_APPROVER_STATUS] " +
                "@Emp_Id=@empId, @Transaction_ID=@txnId, @Version=@ver, " +
                "@Approver_Status=@status, @Remarks=@remarks, @Approver_By=@approver, " +
                "@RowsAffected=@RowsAffected OUTPUT",
                new SqlParameter("@empId", employeeId),
                new SqlParameter("@txnId", transactionId),
                new SqlParameter("@ver", version),
                new SqlParameter("@status", approverStatus),
                new SqlParameter("@remarks", (object?)remarks ?? DBNull.Value),
                new SqlParameter("@approver", (object?)approverBy ?? DBNull.Value),
                rowsAffected);
            return (int)rowsAffected.Value > 0;
        }

        public async Task QueueApproverRejectionEmailAsync(
            string employeeId,
            string? employeeName,
            string category,
            string? vendor,
            string? remarks)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.YMT_EMAIL_QUEUE
                    (EmployeeId, EmployeeName, CategoryCode, Vendor, Remarks, MailType, Status, CreatedOn)
                VALUES
                    ({employeeId}, {employeeName}, {category}, {vendor}, {remarks}, 'APPROVER_REJECTED', 'PENDING', GETDATE())");
        }
    }
}