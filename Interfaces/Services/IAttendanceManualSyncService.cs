namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IAttendanceManualSyncService
    {
        Task<(int Inserted, int Updated, int Total, DateTime SyncedAt)> ManualSyncAsync();
        Task<DateTime?> GetLastSyncAsync();
    }
}