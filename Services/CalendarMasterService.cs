using YMI_PMT_PayrollManagement_API.DTOs.CalendarMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class CalendarMasterService : ICalendarMasterService
    {
        private readonly ICalendarMasterRepository _repository;

        public CalendarMasterService(ICalendarMasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CalendarMasterDTO>> GetByMonthYearAsync(int month, int year)
        {
            var list = await _repository.GetByMonthYearAsync(month, year);
            return list.Select(MapToDto).ToList();
        }

        public async Task<CalendarMasterDTO?> GetByIdAsync(int id)
        {
            var cal = await _repository.GetByIdAsync(id);
            return cal == null ? null : MapToDto(cal);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(CreateCalendarMasterDTO dto)
        {
            if (dto.Day < 1 || dto.Day > 31)
                return (false, "Day must be between 1 and 31", 0);

            if (dto.Month < 1 || dto.Month > 12)
                return (false, "Month must be between 1 and 12", 0);

            if (string.IsNullOrWhiteSpace(dto.HolidayName))
                return (false, "Holiday Name is required", 0);

            if (string.IsNullOrWhiteSpace(dto.HolidayType))
                return (false, "Holiday Type is required", 0);

            // Validate day against actual month length
            int daysInMonth = DateTime.DaysInMonth(dto.Year, dto.Month);
            if (dto.Day > daysInMonth)
                return (false, $"Day {dto.Day} is invalid for {dto.Month}/{dto.Year}", 0);

            if (await _repository.HolidayExistsAsync(dto.Month, dto.Year, dto.Day))
                return (false, "Holiday already exists for this day", 0);

            var entity = new CalendarMaster
            {
                Month = dto.Month,
                Year = dto.Year,
                Day = dto.Day,
                HolidayName = dto.HolidayName.Trim().ToUpper(),
                HolidayType = dto.HolidayType.Trim(),
                CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var id = await _repository.CreateAsync(entity);
            return (true, "Holiday Added Successfully", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateCalendarMasterDTO dto)
        {
            if (dto.Day < 1 || dto.Day > 31)
                return (false, "Day must be between 1 and 31");

            if (dto.Month < 1 || dto.Month > 12)
                return (false, "Month must be between 1 and 12");

            if (string.IsNullOrWhiteSpace(dto.HolidayName))
                return (false, "Holiday Name is required");

            if (string.IsNullOrWhiteSpace(dto.HolidayType))
                return (false, "Holiday Type is required");

            int daysInMonth = DateTime.DaysInMonth(dto.Year, dto.Month);
            if (dto.Day > daysInMonth)
                return (false, $"Day {dto.Day} is invalid for {dto.Month}/{dto.Year}");

            if (await _repository.HolidayExistsAsync(dto.Month, dto.Year, dto.Day, id))
                return (false, "Holiday already exists for this day");

            var entity = new CalendarMaster
            {
                Id = id,
                Month = dto.Month,
                Year = dto.Year,
                Day = dto.Day,
                HolidayName = dto.HolidayName.Trim().ToUpper(),
                HolidayType = dto.HolidayType.Trim(),
                ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var result = await _repository.UpdateAsync(entity);
            return result
                ? (true, "Holiday Updated Successfully")
                : (false, "Holiday not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result
                ? (true, "Deleted Successfully")
                : (false, "Holiday not found");
        }

        /// <summary>
        /// Bulk upsert by Date (Month+Year+Day).
        /// - Does NOT delete other days in the month
        /// - Same day → update name/type
        /// - New day → insert
        /// Used for: Excel upload + Maintenance From–To range
        /// </summary>
        public async Task<(bool Success, string Message)> BulkCreateAsync(BulkCreateCalendarDTO dto)
        {
            if (dto.Holidays == null || dto.Holidays.Count == 0)
                return (false, "No holidays provided");

            if (dto.Month < 1 || dto.Month > 12)
                return (false, "Invalid month");

            int daysInMonth = DateTime.DaysInMonth(dto.Year, dto.Month);
            var seenDays = new HashSet<int>();

            foreach (var h in dto.Holidays)
            {
                // Prefer day from holiday item; fall back to payload month/year
                int day = h.Day;
                int month = h.Month > 0 ? h.Month : dto.Month;
                int year = h.Year > 0 ? h.Year : dto.Year;

                if (day < 1 || day > DateTime.DaysInMonth(year, month))
                    return (false, $"Day {day} is invalid for month {month}/{year}");

                if (string.IsNullOrWhiteSpace(h.HolidayName))
                    return (false, "Holiday Name is required");

                if (string.IsNullOrWhiteSpace(h.HolidayType))
                    return (false, "Holiday Type is required");

                // Duplicate day inside same payload
                int dayKey = year * 10000 + month * 100 + day;
                if (seenDays.Contains(dayKey))
                    return (false, $"Duplicate day {day} in the upload");

                seenDays.Add(dayKey);
            }

            var entities = dto.Holidays.Select(h =>
            {
                int month = h.Month > 0 ? h.Month : dto.Month;
                int year = h.Year > 0 ? h.Year : dto.Year;

                return new CalendarMaster
                {
                    Month = month,
                    Year = year,
                    Day = h.Day,
                    HolidayName = h.HolidayName.Trim().ToUpper(),
                    HolidayType = h.HolidayType.Trim(),
                    CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
                };
            }).ToList();

            await _repository.BulkCreateAsync(entities);

            return (true, $"Imported {dto.Holidays.Count} holiday(s) successfully");
        }

        private static CalendarMasterDTO MapToDto(CalendarMaster c) => new CalendarMasterDTO
        {
            Id = c.Id,
            Month = c.Month,
            Year = c.Year,
            Day = c.Day,
            HolidayName = c.HolidayName ?? string.Empty,
            HolidayType = c.HolidayType ?? string.Empty,
        };
    }
}