using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceSyncController : ControllerBase
    {
        private readonly IAttendanceManualSyncService _service;

        public AttendanceSyncController(IAttendanceManualSyncService service)
        {
            _service = service;
        }

        // GET api/AttendanceSync/last-sync
        [HttpGet("last-sync")]
        public async Task<IActionResult> GetLastSync()
        {
            try
            {
                var last = await _service.GetLastSyncAsync();
                return Ok(new { lastUpdated = last });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed: {ex.Message}" });
            }
        }

        // POST api/AttendanceSync/manual-sync
        [HttpPost("manual-sync")]
        public async Task<IActionResult> ManualSync()
        {
            try
            {
                var r = await _service.ManualSyncAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Attendance synced — {r.Inserted} inserted, {r.Updated} updated",
                    inserted = r.Inserted,
                    updated = r.Updated,
                    total = r.Total,
                    lastUpdated = r.SyncedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}