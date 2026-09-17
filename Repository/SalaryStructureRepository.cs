using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class SalaryStructureRepository : ISalaryStructureRepository
    {
        private readonly AppDbContext _context;

        public SalaryStructureRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalaryStructure>> GetAllAsync()
        {
            return await _context.SalaryStructures
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<SalaryStructure?> GetByIdAsync(int id)
        {
            return await _context.SalaryStructures
                .AsNoTracking()
                .Include(x => x.Components)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.SalaryStructures.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> StructureNameExistsAsync(string structureName, int excludeId = 0)
        {
            var name = (structureName ?? string.Empty).Trim().ToLower();
            if (name.Length == 0) return false;

            return await _context.SalaryStructures
                .AsNoTracking()
                .AnyAsync(x =>
                    x.StructureName != null &&
                    x.StructureName.Trim().ToLower() == name &&
                    x.Id != excludeId);
        }

        public async Task<int> CreateAsync(SalaryStructure entity, List<SalaryStructureComponent> components)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var now = DateTime.Now;

                entity.CreatedOn = now;
                entity.ModifiedOn = null;
                if (string.IsNullOrWhiteSpace(entity.Status)) entity.Status = "1";
                if (string.IsNullOrWhiteSpace(entity.CreatedBy)) entity.CreatedBy = "SYSTEM";

                // Must be empty – otherwise EF inserts children twice
                entity.Components = new List<SalaryStructureComponent>();

                await using var tx = await _context.Database.BeginTransactionAsync();

                _context.SalaryStructures.Add(entity);
                await _context.SaveChangesAsync();          // Id generated

                foreach (var c in components)
                {
                    c.Id = 0;
                    c.StructureId = entity.Id;
                    c.CreatedOn = now;
                    c.CreatedBy = entity.CreatedBy;
                    c.Structure = null;
                }

                if (components.Count > 0)
                {
                    await _context.SalaryStructureComponents.AddRangeAsync(components);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return entity.Id;
            });
        }

        public async Task<bool> UpdateAsync(SalaryStructure entity, List<SalaryStructureComponent> components)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var existing = await _context.SalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == entity.Id);

                if (existing == null) return false;

                var now = DateTime.Now;
                var modifiedBy = string.IsNullOrWhiteSpace(entity.ModifiedBy) ? "SYSTEM" : entity.ModifiedBy;

                await using var tx = await _context.Database.BeginTransactionAsync();

                existing.StructureName = entity.StructureName;
                existing.EmployeeCategory = entity.EmployeeCategory;
                existing.SkillCategory = entity.SkillCategory;
                existing.EffectiveFrom = entity.EffectiveFrom;
                existing.Status = string.IsNullOrWhiteSpace(entity.Status) ? "1" : entity.Status;
                existing.ModifiedOn = now;
                existing.ModifiedBy = modifiedBy;

                // Delete old components first
                var oldComponents = await _context.SalaryStructureComponents
                    .Where(c => c.StructureId == entity.Id)
                    .ToListAsync();

                if (oldComponents.Count > 0)
                    _context.SalaryStructureComponents.RemoveRange(oldComponents);

                await _context.SaveChangesAsync();

                foreach (var c in components)
                {
                    c.Id = 0;
                    c.StructureId = entity.Id;
                    c.CreatedOn = now;
                    c.CreatedBy = modifiedBy;
                    c.ModifiedOn = now;
                    c.ModifiedBy = modifiedBy;
                    c.Structure = null;
                }

                if (components.Count > 0)
                {
                    await _context.SalaryStructureComponents.AddRangeAsync(components);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return true;
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var existing = await _context.SalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existing == null) return false;

                await using var tx = await _context.Database.BeginTransactionAsync();

                var components = await _context.SalaryStructureComponents
                    .Where(c => c.StructureId == id)
                    .ToListAsync();

                if (components.Count > 0)
                    _context.SalaryStructureComponents.RemoveRange(components);

                _context.SalaryStructures.Remove(existing);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return true;
            });
        }
    }
}