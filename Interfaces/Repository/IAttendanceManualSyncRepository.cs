using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Repository
{
    public interface IAttendanceManualSyncRepository
    {
        Task<List<MxVewDailyAttendance>> GetAllFromSourceAsync();
        Task<(int Inserted, int Updated)> BulkUpsertAsync(List<YmtAttendance> records, string user);
        Task<DateTime?> GetLastSyncAsync();
    }
}