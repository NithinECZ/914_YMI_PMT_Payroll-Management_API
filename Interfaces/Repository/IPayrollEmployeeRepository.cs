using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IPayrollEmployeeRepository
    {
        // category = "CL" or "NAPS" (YMT_EMP_MASTER.Ctg_Code)
        Task<List<PayrollEmployee>> GetPayrollEmployeesAsync(string category);

        // Save provision data from uploaded file
        // Returns: number of rows successfully saved
        Task<int> SavePayrollProvisionsAsync(List<PayrollProvisionUploadRowDTO> rows, string category);
    }
}