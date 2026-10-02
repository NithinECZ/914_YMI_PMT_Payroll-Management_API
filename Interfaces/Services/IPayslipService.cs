using YMI_PMT_PayrollManagement_API.DTOs.Payslip;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IPayslipService
    {
        Task<PayslipDataDTO> GetApprovedPayslipDataAsync(string category, DateTime? fromDate, DateTime? toDate);
        Task<List<PayrollPeriodRow>> GetApprovedPeriodsAsync(string category);
        Task<GeneratePayslipResultDTO> GeneratePayslipsAsync(GeneratePayslipRequestDTO dto);
        Task<SendVendorEmailResultDTO> SendVendorEmailsAsync(SendVendorEmailRequestDTO dto);
        Task<List<PayslipEmailStatusDTO>> GetPayslipEmailStatusAsync(string category, DateTime? fromDate, DateTime? toDate);
        Task<List<PayrollProvision>> GetProvisionsAsync(string category);
    }
}
