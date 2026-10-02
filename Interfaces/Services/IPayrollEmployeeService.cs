using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IPayrollEmployeeService
    {
        Task<PayrollDataDTO> GetPayrollEmployeesAsync(string category);

        Task<PayrollDataDTO> GetPayrollEmployeesByStatusAsync(string category, string status, string? stage = null);

        Task<PayrollEmployeeUploadResultDTO> UploadPayrollDataAsync(IFormFile file, string category, string? createdBy = null);

        // user = logged-in user name
        Task<PayrollEmployeeUploadResultDTO> CalculatePayrollAsync(string category, string? user = null);

        Task<PayrollEmployeeUploadResultDTO> SubmitPayrollAsync(string category, string? user = null);

        Task<PayrollCheckerDataDTO> GetPayrollCheckerDataAsync(string category);
        Task<PayrollCheckerDetailDTO?> GetPayrollCheckerDetailAsync(int id, string employeeId, string category);

        Task<PayrollEmployeeUploadResultDTO> UpdateCheckerStatusAsync(PayrollCheckerStatusUpdateDTO dto);

        Task<PayrollDataDTO> GetPayrollAnalysisAsync(string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to);
        Task<PayrollEmployeeUploadResultDTO> RaiseResignQueryAsync(PayrollResignRequestDTO dto);
        Task<List<PayrollPeriodSummaryDTO>> GetPayrollPeriodsAsync(string category);
        Task<PayrollDataDTO> GetPayrollHistoryAsync(string category, DateTime from, DateTime to);
        Task<PayrollDataDTO> GetPayrollLiveAttendanceAsync(string category);

        Task<PayrollStatusSummaryDTO> GetPayrollStatusSummaryAsync(string category);
    }
}