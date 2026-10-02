using YMI_PMT_PayrollManagement_API.DTOs.Payslip;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class PayslipService : IPayslipService
    {
        private readonly IPayslipRepository _repository;

        public PayslipService(IPayslipRepository repository)
        {
            _repository = repository;
        }

        public async Task<PayslipDataDTO> GetApprovedPayslipDataAsync(string category, DateTime? fromDate, DateTime? toDate)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var rows = await _repository.GetApprovedPayslipDataAsync(cat, fromDate, toDate);

            return new PayslipDataDTO
            {
                Employees = rows
            };
        }

        public async Task<List<PayrollPeriodRow>> GetApprovedPeriodsAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            return await _repository.GetApprovedPeriodsAsync(cat);
        }

        public async Task<List<PayrollProvision>> GetProvisionsAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            return await _repository.GetProvisionsAsync(cat);
        }

        public async Task<GeneratePayslipResultDTO> GeneratePayslipsAsync(GeneratePayslipRequestDTO dto)
        {
            var result = new GeneratePayslipResultDTO();
            if (dto == null || dto.Employees == null || dto.Employees.Count == 0)
            {
                result.Success = false;
                result.Message = "No employees selected for payslip generation";
                return result;
            }

            int count = 0;
            foreach (var emp in dto.Employees)
            {
                var ok = await _repository.RecordPayslipGenerationAsync(
                    emp.EmployeeId,
                    emp.TransactionId,
                    emp.Version,
                    dto.Category,
                    dto.GeneratedBy ?? "System"
                );
                if (ok) count++;
            }

            result.Success = true;
            result.ProcessedCount = count;
            result.Message = $"Successfully generated payslips for {count} employee(s)";
            return result;
        }

        public async Task<SendVendorEmailResultDTO> SendVendorEmailsAsync(SendVendorEmailRequestDTO dto)
        {
            return await _repository.SendVendorEmailsAsync(dto);
        }

        public async Task<List<PayslipEmailStatusDTO>> GetPayslipEmailStatusAsync(string category, DateTime? fromDate, DateTime? toDate)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            return await _repository.GetPayslipEmailStatusAsync(cat, fromDate, toDate);
        }
    }
}
