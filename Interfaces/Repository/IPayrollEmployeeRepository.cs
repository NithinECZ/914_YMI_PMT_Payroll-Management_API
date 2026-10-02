using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IPayrollEmployeeRepository
    {
        Task<List<PayrollEmployee>> GetPayrollEmployeesAsync(string category);

        // 👈 CHANGED: added 'stage' parameter
        Task<List<PayrollEmployee>> GetPayrollEmployeesByStatusAsync(string category, string status, string? stage);

        Task<int> SavePayrollProvisionsAsync(
            List<PayrollProvisionSaveRowDTO> rows,
            string category,
            DateTime? fromDate,
            DateTime? toDate,
            string? createdBy);

        Task<List<PayrollProvision>> GetPayrollProvisionsAsync(string category);

        Task<(int Processed, int NoStructure)> CalculatePayrollAsync(string category, string? user = null);

        Task<int> SubmitPayrollAsync(string category, string? user = null);

        Task<int> SavePayrollAttendanceAsync(string category, string? user = null);

        Task<List<PayrollCalculation>> GetPayrollCalculationsAsync(string category);
        Task<List<PayrollCheckerRow>> GetPayrollCheckerDataAsync(string category);
        Task<List<PayrollEmployee>> GetPayrollCheckerAttendanceAsync(string employeeId, string category);

        // Analysis (previous period) data
        Task<List<PayrollEmployee>> GetPayrollAnalysisAsync(string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to);
        Task<List<PayrollCalculation>> GetPayrollAnalysisCalculationsAsync(string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to);
        Task QueueAdminResignEmailAsync(string employeeId, string? employeeName, string category, string? vendor, string? remarks);
        Task<List<EmpLeavingRow>> GetEmployeeLeavingDatesAsync();

        Task<List<PayrollPeriodRow>> GetPayrollPeriodsAsync(string category);
        Task<List<PayrollEmployee>> GetPayrollHistoryAsync(string category, DateTime from, DateTime to);
        Task<List<PayrollCalculation>> GetPayrollHistoryCalculationsAsync(string category, DateTime from, DateTime to);
        Task<List<PayrollProvision>> GetPayrollHistoryProvisionsAsync(string category, DateTime from, DateTime to);
        Task<List<PayrollEmployee>> GetPayrollLiveAttendanceAsync(string category);

        // 👈 CHANGED: return type is now List<PayrollStageRow>
        Task<List<PayrollStageRow>> GetPayrollStatusSummaryAsync(string category);

        Task<bool> UpdateCheckerStatusAsync(
            string employeeId,
            string transactionId,
            string version,
            string category,
            string approverStatus,
            string? remarks,
            string? checkerBy);

        Task QueueRejectionEmailAsync(
            string employeeId,
            string? employeeName,
            string category,
            string? vendor,
            string? remarks);
    }
}