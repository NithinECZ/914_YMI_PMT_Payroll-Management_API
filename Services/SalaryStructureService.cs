using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YMI_PMT_PayrollManagement_API.DTOs.SalaryStructure;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class SalaryStructureService : ISalaryStructureService
    {
        private readonly ISalaryStructureRepository _repository;

        private static readonly string[] ValidTypes = { "Earning", "Deduction" };
        private static readonly string[] ValidMethods = { "Fixed Amount", "Percentage", "Per Hour" };

        public SalaryStructureService(ISalaryStructureRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SalaryStructureDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToListDto).ToList();
        }

        public async Task<SalaryStructureDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : MapToDetailDto(entity);
        }

        public async Task<(bool Success, string Message, string? Field, int Id)> CreateAsync(CreateSalaryStructureDTO dto)
        {
            var (ok, msg, field) = await ValidateAsync(dto, 0);
            if (!ok) return (false, msg, field, 0);

            var entity = BuildEntity(dto, 0);
            entity.CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy.Trim();

            var components = BuildComponents(dto);

            var id = await _repository.CreateAsync(entity, components);
            return (true, "Salary Structure Saved Successfully", null, id);
        }

        public async Task<(bool Success, string Message, string? Field)> UpdateAsync(int id, CreateSalaryStructureDTO dto)
        {
            if (id <= 0)
                return (false, "Invalid Salary Structure id", null);

            if (!await _repository.ExistsAsync(id))
                return (false, "Salary Structure not found", null);

            var (ok, msg, field) = await ValidateAsync(dto, id);
            if (!ok) return (false, msg, field);

            var entity = BuildEntity(dto, id);
            entity.ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy.Trim();

            var components = BuildComponents(dto);

            var result = await _repository.UpdateAsync(entity, components);
            return result
                ? (true, "Salary Structure Updated Successfully", null)
                : (false, "Salary Structure not found", null);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result
                ? (true, "Deleted Successfully")
                : (false, "Salary Structure not found");
        }

        private async Task<(bool Ok, string Message, string? Field)> ValidateAsync(CreateSalaryStructureDTO dto, int idBeingEdited)
        {
            if (dto == null)
                return (false, "Request body is empty", null);

            var name = (dto.StructureName ?? string.Empty).Trim();

            if (name.Length == 0)
                return (false, "Structure Name is required", "structureName");

            if (name.Length > 150)
                return (false, "Structure Name cannot be longer than 150 characters", "structureName");

            if (string.IsNullOrWhiteSpace(dto.EmployeeCategory))
                return (false, "Employee Group is required", "employeeCategory");

            if (string.IsNullOrWhiteSpace(dto.SkillCategory))
                return (false, "Skill Category is required", "skillCategory");

            if (string.IsNullOrWhiteSpace(dto.EffectiveFrom))
                return (false, "Effective From is required", "effectiveFrom");

            if (!TryParseDate(dto.EffectiveFrom, out _))
                return (false, "Effective From is not a valid date", "effectiveFrom");

            if (dto.Components == null || dto.Components.Count == 0)
                return (false, "At least one component is required", null);

            foreach (var c in dto.Components)
            {
                if (string.IsNullOrWhiteSpace(c.ComponentName))
                    return (false, "Component Name is required for every component", null);

                if (!ValidTypes.Contains((c.Type ?? string.Empty).Trim()))
                    return (false, $"'{c.ComponentName}' has an invalid Type", null);

                if (!ValidMethods.Contains((c.CalculationMethod ?? string.Empty).Trim()))
                    return (false, $"'{c.ComponentName}' has an invalid Calculation Method", null);

                if (c.AmountOrPercentage < 0)
                    return (false, $"'{c.ComponentName}' cannot have a negative amount", null);

                if (string.Equals((c.CalculationMethod ?? string.Empty).Trim(), "Percentage", StringComparison.OrdinalIgnoreCase)
                    && c.AmountOrPercentage > 100)
                    return (false, $"'{c.ComponentName}' percentage cannot be more than 100", null);
            }

            var dupComponent = dto.Components
                .GroupBy(c => (c.ComponentName ?? string.Empty).Trim().ToLowerInvariant())
                .FirstOrDefault(g => g.Count() > 1);

            if (dupComponent != null)
                return (false, $"Component '{dupComponent.First().ComponentName}' is added more than once", null);

            if (await _repository.StructureNameExistsAsync(name, idBeingEdited))
                return (false, $"Structure Name '{name}' already exists", "structureName");

            return (true, string.Empty, null);
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out result)
                   || DateTime.TryParse(value, CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out result);
        }

        private static SalaryStructure BuildEntity(CreateSalaryStructureDTO dto, int id)
        {
            TryParseDate(dto.EffectiveFrom, out var effectiveFrom);

            return new SalaryStructure
            {
                Id = id,
                StructureName = dto.StructureName.Trim(),
                EmployeeCategory = dto.EmployeeCategory?.Trim(),
                SkillCategory = dto.SkillCategory?.Trim(),
                EffectiveFrom = effectiveFrom == default ? null : effectiveFrom,
                Status = dto.Status == "0" ? "0" : "1",
            };
        }

        private static List<SalaryStructureComponent> BuildComponents(CreateSalaryStructureDTO dto)
        {
            return (dto.Components ?? new List<CreateSalaryStructureComponentDTO>())
                .Select(c => new SalaryStructureComponent
                {
                    ComponentName = c.ComponentName?.Trim(),
                    Type = c.Type?.Trim(),
                    CalculationMethod = c.CalculationMethod?.Trim(),
                    AmountOrPercentage = c.AmountOrPercentage
                })
                .ToList();
        }

        private static SalaryStructureDTO MapToListDto(SalaryStructure s) => new SalaryStructureDTO
        {
            Id = s.Id,
            StructureName = s.StructureName ?? string.Empty,
            EmployeeCategory = s.EmployeeCategory ?? string.Empty,
            SkillCategory = s.SkillCategory ?? string.Empty,
            EffectiveFrom = s.EffectiveFrom?.ToString("yyyy-MM-dd"),
            Status = string.IsNullOrWhiteSpace(s.Status) ? "1" : s.Status,
            CreatedBy = s.CreatedBy ?? string.Empty,
            CreatedOn = s.CreatedOn?.ToString("yyyy-MM-dd"),
            ModifiedBy = s.ModifiedBy ?? string.Empty,
            ModifiedOn = s.ModifiedOn?.ToString("yyyy-MM-dd"),
        };

        private static SalaryStructureDTO MapToDetailDto(SalaryStructure s)
        {
            var dto = MapToListDto(s);
            dto.Components = (s.Components ?? new List<SalaryStructureComponent>())
                .OrderBy(c => c.Id)
                .Select(c => new SalaryStructureComponentDTO
                {
                    Id = c.Id,
                    ComponentName = c.ComponentName ?? string.Empty,
                    Type = c.Type ?? string.Empty,
                    CalculationMethod = c.CalculationMethod ?? string.Empty,
                    AmountOrPercentage = c.AmountOrPercentage ?? 0
                })
                .ToList();
            return dto;
        }
    }
}