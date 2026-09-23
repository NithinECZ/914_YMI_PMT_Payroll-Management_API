using YMI_PMT_PayrollManagement_API.DTOs.SalaryFormula;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class SalaryFormulaService : ISalaryFormulaService
    {
        private readonly ISalaryFormulaRepository _repository;

        public SalaryFormulaService(ISalaryFormulaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SalaryFormulaDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<SalaryFormulaDTO?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : MapToDto(item);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(CreateSalaryFormulaDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FromDate))
                return (false, "From Date is required", 0);
            if (string.IsNullOrWhiteSpace(dto.ToDate))
                return (false, "To Date is required", 0);

            if (!DateTime.TryParse(dto.FromDate, out var fromDt))
                return (false, "Invalid From Date", 0);
            if (!DateTime.TryParse(dto.ToDate, out var toDt))
                return (false, "Invalid To Date", 0);

            if (fromDt > toDt)
                return (false, "To Date must be after From Date", 0);

            var entity = new SalaryFormula
            {
                FromDate = fromDt,
                ToDate = toDt,
                Status = dto.IsActive ? "1" : "0",
                CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy
            };

            var id = await _repository.CreateAsync(entity);
            return (true, "Saved Successfully", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateSalaryFormulaDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FromDate))
                return (false, "From Date is required");
            if (string.IsNullOrWhiteSpace(dto.ToDate))
                return (false, "To Date is required");

            if (!DateTime.TryParse(dto.FromDate, out var fromDt))
                return (false, "Invalid From Date");
            if (!DateTime.TryParse(dto.ToDate, out var toDt))
                return (false, "Invalid To Date");

            if (fromDt > toDt)
                return (false, "To Date must be after From Date");

            var entity = new SalaryFormula
            {
                Id = id,
                FromDate = fromDt,
                ToDate = toDt,
                Status = dto.IsActive ? "1" : "0",
                ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy
            };

            var result = await _repository.UpdateAsync(entity);
            return result ? (true, "Updated Successfully") : (false, "Record not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Deleted Successfully") : (false, "Record not found");
        }

        private static SalaryFormulaDTO MapToDto(SalaryFormula x) => new SalaryFormulaDTO
        {
            Id = x.Id,
            TrId = x.TrId ?? string.Empty,
            FromDate = x.FromDate.ToString("yyyy-MM-dd"),
            ToDate = x.ToDate.ToString("yyyy-MM-dd"),
            Status = x.Status ?? "0",
            CreatedBy = x.CreatedBy,
            CreatedOn = x.CreatedOn,
            ModifiedBy = x.ModifiedBy,
            ModifiedOn = x.ModifiedOn
        };
    }
}