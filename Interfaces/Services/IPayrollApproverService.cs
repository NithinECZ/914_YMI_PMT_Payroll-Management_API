using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IPayrollApproverService
    {
        Task<PayrollApproverDataDTO> GetPayrollApproverDataAsync(string category);
        Task<PayrollApproverDetailDTO?> GetPayrollApproverDetailAsync(int id, string employeeId, string category);
        Task<PayrollEmployeeUploadResultDTO> UpdateApproverStatusAsync(PayrollApproverStatusUpdateDTO dto);
    }
}