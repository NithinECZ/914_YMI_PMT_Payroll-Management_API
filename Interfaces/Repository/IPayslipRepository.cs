using YMI_PMT_PayrollManagement_API.DTOs.Payslip;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IPayslipRepository
    {
        Task<List<PayslipEmployeeDTO>> GetApprovedPayslipDataAsync(string category, DateTime? fromDate, DateTime? toDate);
        Task<List<PayrollPeriodRow>> GetApprovedPeriodsAsync(string category);
        Task<bool> RecordPayslipGenerationAsync(string employeeId, string transactionId, string version, string category, string generatedBy);
        Task<SendVendorEmailResultDTO> SendVendorEmailsAsync(SendVendorEmailRequestDTO dto);
        Task<List<PayslipEmailStatusDTO>> GetPayslipEmailStatusAsync(string category, DateTime? fromDate, DateTime? toDate);
        Task<List<PayrollProvision>> GetProvisionsAsync(string category);
    }
}
