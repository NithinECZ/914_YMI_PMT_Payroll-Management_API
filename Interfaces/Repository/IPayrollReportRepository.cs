using System.Collections.Generic;
using System.Threading.Tasks;
using YMI_PMT_PayrollManagement_API.DTOs.Report;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IPayrollReportRepository
    {
        Task<List<PayrollReportRowDTO>> GetPayrollReportAsync(PayrollReportFilterDTO filter);
        Task<PayrollReportFilterOptionsDTO> GetFilterOptionsAsync(string category);
    }
}
