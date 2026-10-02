using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IPayrollApproverRepository
    {
        Task<List<PayrollCheckerRow>> GetPayrollApproverDataAsync(string category);
        Task<List<PayrollEmployee>> GetPayrollApproverAttendanceAsync(string employeeId, string category);

        Task<bool> UpdateApproverStatusAsync(
            string employeeId,
            string transactionId,
            string version,
            string category,
            string approverStatus,
            string? remarks,
            string? approverBy);

        Task QueueApproverRejectionEmailAsync(
            string employeeId,
            string? employeeName,
            string category,
            string? vendor,
            string? remarks);
    }
}