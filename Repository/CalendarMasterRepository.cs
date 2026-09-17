using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Data;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class CalendarMasterRepository : ICalendarMasterRepository
    {
        private readonly AppDbContext _context;

        public CalendarMasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CalendarMaster>> GetByMonthYearAsync(int month, int year)
        {
            return await _context.CalendarMasters
                .AsNoTracking()
                .Where(c => c.Month == month && c.Year == year)
                .OrderBy(c => c.Day)
                .ToListAsync();
        }

        public async Task<CalendarMaster?> GetByIdAsync(int id)
        {
            return await _context.CalendarMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CalendarMaster?> GetByMonthYearDayAsync(int month, int year, int day)
        {
            return await _context.CalendarMasters
                .FirstOrDefaultAsync(c => c.Month == month && c.Year == year && c.Day == day);
        }

        public async Task<int> CreateAsync(CalendarMaster calendar)
        {
            if (calendar.Day < 1 || calendar.Day > 31)
                throw new ArgumentException("Day must be between 1 and 31");
            if (calendar.Month < 1 || calendar.Month > 12)
                throw new ArgumentException("Month must be between 1 and 12");
            if (string.IsNullOrEmpty(calendar.HolidayName))
                throw new ArgumentException("Holiday Name is required");
            if (string.IsNullOrEmpty(calendar.HolidayType))
                throw new ArgumentException("Holiday Type is required");

            calendar.CreatedOn = DateTime.Now;
            if (string.IsNullOrEmpty(calendar.CreatedBy))
                calendar.CreatedBy = "SYSTEM";

            _context.CalendarMasters.Add(calendar);
            await _context.SaveChangesAsync();
            return calendar.Id;
        }

        public async Task<bool> UpdateAsync(CalendarMaster calendar)
        {
            var existing = await _context.CalendarMasters
                .FirstOrDefaultAsync(c => c.Id == calendar.Id);

            if (existing == null)
                return false;

            existing.Month = calendar.Month;
            existing.Year = calendar.Year;
            existing.Day = calendar.Day;
            existing.HolidayName = calendar.HolidayName;
            existing.HolidayType = calendar.HolidayType;
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = calendar.ModifiedBy ?? "SYSTEM";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.CalendarMasters
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existing == null)
                return false;

            _context.CalendarMasters.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HolidayExistsAsync(int month, int year, int day, int excludeId = 0)
        {
            return await _context.CalendarMasters
                .AnyAsync(c =>
                    c.Month == month &&
                    c.Year == year &&
                    c.Day == day &&
                    c.Id != excludeId);
        }

        public async Task<List<CalendarMaster>> GetAllAsync()
        {
            return await _context.CalendarMasters
                .AsNoTracking()
                .OrderBy(c => c.Year)
                .ThenBy(c => c.Month)
                .ThenBy(c => c.Day)
                .ToListAsync();
        }

        /// <summary>
        /// Upsert: if same Month+Year+Day exists → update, else insert.
        /// Does NOT wipe other holidays in the month.
        /// </summary>
        public async Task BulkCreateAsync(List<CalendarMaster> calendars)
        {
            if (calendars == null || calendars.Count == 0)
                return;

            foreach (var cal in calendars)
            {
                var existing = await _context.CalendarMasters
                    .FirstOrDefaultAsync(c =>
                        c.Month == cal.Month &&
                        c.Year == cal.Year &&
                        c.Day == cal.Day);

                if (existing != null)
                {
                    // Update existing day
                    existing.HolidayName = cal.HolidayName;
                    existing.HolidayType = cal.HolidayType;
                    existing.ModifiedOn = DateTime.Now;
                    existing.ModifiedBy = cal.CreatedBy ?? cal.ModifiedBy ?? "SYSTEM";
                }
                else
                {
                    // Insert new day
                    cal.CreatedOn = DateTime.Now;
                    if (string.IsNullOrEmpty(cal.CreatedBy))
                        cal.CreatedBy = "SYSTEM";
                    await _context.CalendarMasters.AddAsync(cal);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteByMonthYearAsync(int month, int year)
        {
            var existing = await _context.CalendarMasters
                .Where(c => c.Month == month && c.Year == year)
                .ToListAsync();

            if (existing.Any())
            {
                _context.CalendarMasters.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }
        }
    }
}