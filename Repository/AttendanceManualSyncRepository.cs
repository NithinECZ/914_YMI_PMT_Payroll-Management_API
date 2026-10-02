using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class AttendanceManualSyncRepository : IAttendanceManualSyncRepository
    {
        private readonly SourceDbContext _source;
        private readonly AppDbContext _target;

        public AttendanceManualSyncRepository(SourceDbContext source, AppDbContext target)
        {
            _source = source;
            _target = target;
        }

        public async Task<List<MxVewDailyAttendance>> GetAllFromSourceAsync()
        {
            _source.Database.SetCommandTimeout(300);
            return await _source.MxVewDailyAttendance
                .FromSqlRaw("EXEC dbo.usp_GetAttendanceForSync")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(int Inserted, int Updated)> BulkUpsertAsync(List<YmtAttendance> records, string user)
        {
            if (records == null || records.Count == 0) return (0, 0);

            int inserted = 0, updated = 0;
            var empIds = records.Select(r => r.Emp_Id).Distinct().ToList();
            var pDates = records.Select(r => r.P_Date).Distinct().ToList();

            var existing = await _target.YmtAttendances
                .Where(x => empIds.Contains(x.Emp_Id) && pDates.Contains(x.P_Date))
                .ToListAsync();

            var dict = existing
                .GroupBy(x => (x.Emp_Id, x.P_Date))
                .ToDictionary(g => g.Key, g => g.First());

            var now = DateTime.Now;

            foreach (var rec in records)
            {
                if (dict.TryGetValue((rec.Emp_Id, rec.P_Date), out var e))
                {
                    e.Emp_Nm = rec.Emp_Nm;
                    e.Punch1 = rec.Punch1; e.P1_Date = rec.P1_Date; e.P1_Time = rec.P1_Time;
                    e.Punch2 = rec.Punch2; e.P2_Date = rec.P2_Date; e.P2_Time = rec.P2_Time;
                    e.Sch_Shift = rec.Sch_Shift; e.Wrk_Shift = rec.Wrk_Shift;
                    e.Erly_In = rec.Erly_In; e.ErIn_HM = rec.ErIn_HM;
                    e.Late_In = rec.Late_In; e.LtIn_HM = rec.LtIn_HM;
                    e.Erly_Out = rec.Erly_Out; e.ErOut_HM = rec.ErOut_HM;
                    e.Ovr_Stay = rec.Ovr_Stay; e.OvSty_HM = rec.OvSty_HM;
                    e.WkTm_HM = rec.WkTm_HM; e.Fst_Half = rec.Fst_Half; e.Snd_Half = rec.Snd_Half;
                    e.Sft_Strt = rec.Sft_Strt; e.Sft_End = rec.Sft_End;
                    e.MinF_Hrs = rec.MinF_Hrs; e.MinF_HHM = rec.MinF_HHM;
                    e.MinH_Hrs = rec.MinH_Hrs; e.MinH_HHM = rec.MinH_HHM;
                    e.Net_Wrk = rec.Net_Wrk; e.Summary = rec.Summary; e.OutTm_HM = rec.OutTm_HM;
                    e.WSft_Hrs = rec.WSft_Hrs; e.WSft_HHM = rec.WSft_HHM;
                    e.Mdfd_On = now;
                    e.Mdfd_By = user;
                    updated++;
                }
                else
                {
                    rec.Status = "1";
                    rec.Crtd_On = now;
                    rec.Crtd_By = user;
                    _target.YmtAttendances.Add(rec);
                    dict[(rec.Emp_Id, rec.P_Date)] = rec;   // same batch duplicates
                    inserted++;
                }
            }

            await _target.SaveChangesAsync();
            _target.ChangeTracker.Clear();
            return (inserted, updated);
        }

        public async Task<DateTime?> GetLastSyncAsync()
        {
            return await _target.YmtAttendances
                .AsNoTracking()
                .MaxAsync(x => (DateTime?)(x.Mdfd_On ?? x.Crtd_On));
        }
    }
}