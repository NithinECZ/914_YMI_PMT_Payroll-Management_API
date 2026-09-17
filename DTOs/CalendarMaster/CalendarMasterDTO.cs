namespace YMI_PMT_PayrollManagement_API.DTOs.CalendarMaster
{
    public class CalendarMasterDTO
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Day { get; set; }
        public string HolidayName { get; set; } = string.Empty;
        public string HolidayType { get; set; } = string.Empty;
    }

    public class CreateCalendarMasterDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int Day { get; set; }
        public string HolidayName { get; set; } = string.Empty;
        public string HolidayType { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = "SYSTEM";
    }

    public class BulkCreateCalendarDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<BulkHolidayDTO> Holidays { get; set; } = new();
        public string ModifiedBy { get; set; } = "SYSTEM";
    }

    public class BulkHolidayDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int Day { get; set; }
        public string HolidayName { get; set; } = string.Empty;
        public string HolidayType { get; set; } = string.Empty;
    }
}