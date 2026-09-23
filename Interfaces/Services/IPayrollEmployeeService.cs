using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IPayrollEmployeeService
    {
        // category = "CL" or "NAPS"
        Task<PayrollDataDTO> GetPayrollEmployeesAsync(string category);

        // Upload provisions data from CSV/Excel file
        Task<PayrollEmployeeUploadResultDTO> UploadPayrollDataAsync(IFormFile file, string category);
    }
}