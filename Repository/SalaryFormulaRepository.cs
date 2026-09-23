using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class SalaryFormulaRepository : ISalaryFormulaRepository
    {
        private readonly AppDbContext _context;

        public SalaryFormulaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalaryFormula>> GetAllAsync()
        {
            return await _context.SalaryFormulas
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<SalaryFormula?> GetByIdAsync(int id)
        {
            return await _context.SalaryFormulas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<int> CreateAsync(SalaryFormula entity)
        {
            entity.CreatedOn = DateTime.Now;
            if (string.IsNullOrEmpty(entity.Status))
                entity.Status = "1";
            if (string.IsNullOrEmpty(entity.CreatedBy))
                entity.CreatedBy = "SYSTEM";

            // Generate TR_ID: FR + Year + Sequential
            var year = DateTime.Now.Year;
            var count = await _context.SalaryFormulas.CountAsync(x => x.TrId!.StartsWith($"FR{year}")) + 1;
            entity.TrId = $"FR{year}{count:D8}";

            _context.SalaryFormulas.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(SalaryFormula entity)
        {
            var existing = await _context.SalaryFormulas.FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (existing == null) return false;

            existing.FromDate = entity.FromDate;
            existing.ToDate = entity.ToDate;
            existing.Status = entity.Status ?? "1";
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = entity.ModifiedBy ?? "SYSTEM";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.SalaryFormulas.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return false;

            _context.SalaryFormulas.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}