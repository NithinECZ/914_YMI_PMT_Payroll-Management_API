using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class AttendanceManualSyncService : IAttendanceManualSyncService
    {
        private const int BatchSize = 2000;
        private const string SyncUser = "MANUAL";
        private static readonly SemaphoreSlim _lock = new(1, 1); // double-click protection

        private readonly IAttendanceManualSyncRepository _repo;
        private readonly ILogger<AttendanceManualSyncService> _logger;

        public AttendanceManualSyncService(
            IAttendanceManualSyncRepository repo,
            ILogger<AttendanceManualSyncService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<(int Inserted, int Updated, int Total, DateTime SyncedAt)> ManualSyncAsync()
        {
            if (!await _lock.WaitAsync(0))
                throw new InvalidOperationException("A manual sync is already running. Please wait.");

            try
            {
                var source = await _repo.GetAllFromSourceAsync();

                var entities = source
                    .Where(s => !string.IsNullOrWhiteSpace(s.Emp_Id) && !string.IsNullOrWhiteSpace(s.P_Date))
                    .Select(MapToEntity)
                    .ToList();

                int ins = 0, upd = 0;
                for (int i = 0; i < entities.Count; i += BatchSize)
                {
                    var batch = entities.Skip(i).Take(BatchSize).ToList();
                    var (a, b) = await _repo.BulkUpsertAsync(batch, SyncUser);
                    ins += a; upd += b;
                }

                _logger.LogInformation("Manual attendance sync done. Inserted={Ins}, Updated={Upd}, Total={Tot}",
                    ins, upd, entities.Count);

                return (ins, upd, entities.Count, DateTime.Now);
            }
            finally
            {
                _lock.Release();
            }
        }

        public Task<DateTime?> GetLastSyncAsync() => _repo.GetLastSyncAsync();

        private static YmtAttendance MapToEntity(MxVewDailyAttendance s) => new()
        {
            Emp_Id = s.Emp_Id.Trim(),
            Emp_Nm = s.Emp_Nm,
            P_Date = s.P_Date!,
            Punch1 = s.Punch1,
            P1_Date = s.P1_Date,
            P1_Time = s.P1_Time,
            Punch2 = s.Punch2,
            P2_Date = s.P2_Date,
            P2_Time = s.P2_Time,
            Sch_Shift = s.Sch_Shift,
            Wrk_Shift = s.Wrk_Shift,
            Erly_In = s.Erly_In,
            ErIn_HM = s.ErIn_HM,
            Late_In = s.Late_In,
            LtIn_HM = s.LtIn_HM,
            Erly_Out = s.Erly_Out,
            ErOut_HM = s.ErOut_HM,
            Ovr_Stay = s.Ovr_Stay,
            OvSty_HM = s.OvSty_HM,
            WkTm_HM = s.WkTm_HM,
            Fst_Half = s.Fst_Half,
            Snd_Half = s.Snd_Half,
            Sft_Strt = s.Sft_Strt,
            Sft_End = s.Sft_End,
            MinF_Hrs = s.MinF_Hrs,
            MinF_HHM = s.MinF_HHM,
            MinH_Hrs = s.MinH_Hrs,
            MinH_HHM = s.MinH_HHM,
            Net_Wrk = s.Net_Wrk,
            Summary = s.Summary,
            OutTm_HM = s.OutTm_HM,
            WSft_Hrs = s.WSft_Hrs,
            WSft_HHM = s.WSft_HHM
        };
    }
}