using YMI_PMT_PayrollManagement_API.DTOs.CalendarMaster;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface ICalendarMasterService
    {
        Task<List<CalendarMasterDTO>> GetByMonthYearAsync(int month, int year);
        Task<CalendarMasterDTO?> GetByIdAsync(int id);
        Task<(bool Success, string Message, int Id)> CreateAsync(CreateCalendarMasterDTO dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, CreateCalendarMasterDTO dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
        Task<(bool Success, string Message)> BulkCreateAsync(BulkCreateCalendarDTO dto);
    }
}