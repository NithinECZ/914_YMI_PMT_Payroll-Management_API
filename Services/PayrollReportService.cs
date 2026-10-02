using System.Collections.Generic;
using System.Threading.Tasks;
using YMI_PMT_PayrollManagement_API.DTOs.Report;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class PayrollReportService : IPayrollReportService
    {
        private readonly IPayrollReportRepository _repository;

        public PayrollReportService(IPayrollReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PayrollReportRowDTO>> GetPayrollReportAsync(PayrollReportFilterDTO filter)
        {
            filter.Category = string.IsNullOrWhiteSpace(filter.Category) ? "CL" : filter.Category.Trim().ToUpperInvariant();
            return await _repository.GetPayrollReportAsync(filter);
        }

        public async Task<PayrollReportFilterOptionsDTO> GetFilterOptionsAsync(string category)
        {
            var cat = string.IsNullOrWhiteSpace(category) ? "CL" : category.Trim().ToUpperInvariant();
            return await _repository.GetFilterOptionsAsync(cat);
        }
    }
}
