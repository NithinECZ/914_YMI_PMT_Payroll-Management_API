using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface ICalendarMasterRepository
    {
        Task<List<CalendarMaster>> GetByMonthYearAsync(int month, int year);
        Task<CalendarMaster?> GetByIdAsync(int id);
        Task<int> CreateAsync(CalendarMaster calendar);
        Task<bool> UpdateAsync(CalendarMaster calendar);
        Task<bool> DeleteAsync(int id);
        Task<bool> HolidayExistsAsync(int month, int year, int day, int excludeId = 0);
        Task<List<CalendarMaster>> GetAllAsync();
        Task BulkCreateAsync(List<CalendarMaster> calendars);
        Task DeleteByMonthYearAsync(int month, int year);
        Task<CalendarMaster?> GetByMonthYearDayAsync(int month, int year, int day);
    }
}