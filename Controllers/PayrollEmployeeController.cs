using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollEmployeeController : ControllerBase
    {
        private readonly IPayrollEmployeeService _service;

        public PayrollEmployeeController(IPayrollEmployeeService service)
        {
            _service = service;
        }

        // Logged-in user name: frontend value first, then auth identity, else SYSTEM
        private string ResolveUser(string? fromClient)
        {
            if (!string.IsNullOrWhiteSpace(fromClient)) return fromClient.Trim();
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
                return User.Identity.Name!;
            return "SYSTEM";
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.GetPayrollEmployeesAsync(cat);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to load payroll data: {ex.Message}");
            }
        }

        [HttpGet("by-status")]
        public async Task<IActionResult> GetByStatus(
            [FromQuery] string category = "CL",
            [FromQuery] string status = "ALL",
            [FromQuery] string? stage = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var st = (status ?? "ALL").Trim().ToUpperInvariant();
            if (st != "ALL" && st != "VERIFY" && st != "REJECT" && st != "HOLD" && st != "APPROVE")
                return BadRequest("status must be ALL | VERIFY | REJECT | HOLD | APPROVE");

            try
            {
                var result = await _service.GetPayrollEmployeesByStatusAsync(cat, st, stage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to load {st} records: {ex.Message}");
            }
        }

        [HttpGet("analysis")]
        public async Task<IActionResult> GetAnalysis(
            [FromQuery] string category = "CL",
            [FromQuery] string? search = null,
            [FromQuery] string? skill = null,
            [FromQuery] string? vendor = null,
            [FromQuery(Name = "from")] string? fromDate = null,
            [FromQuery(Name = "to")] string? toDate = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var q = (search ?? string.Empty).Trim();
            var sk = (skill ?? string.Empty).Trim();
            var vn = (vendor ?? string.Empty).Trim();

            if (q.Length == 0 && sk.Length == 0 && vn.Length == 0)
                return BadRequest("Provide at least one of: search, skill, vendor");

            DateTime? f = null, t = null;
            if (!string.IsNullOrWhiteSpace(fromDate) || !string.IsNullOrWhiteSpace(toDate))
            {
                if (!DateTime.TryParseExact(fromDate?.Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd) ||
                    !DateTime.TryParseExact(toDate?.Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var td))
                    return BadRequest("from and to must be yyyy-MM-dd");
                f = fd; t = td;
            }

            try
            {
                var result = await _service.GetPayrollAnalysisAsync(
                    cat,
                    q.Length == 0 ? null : q,
                    sk.Length == 0 ? null : sk,
                    vn.Length == 0 ? null : vn,
                    f, t);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Analysis failed: {ex.Message}");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadPayrollData()
        {
            var file = Request.Form.Files.FirstOrDefault();
            var category = Request.Form["category"].ToString();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (string.IsNullOrWhiteSpace(category) || (category != "CL" && category != "NAPS"))
                return BadRequest("Invalid or missing category (must be CL or NAPS)");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                return BadRequest("File must be .xlsx or .xls");

            var createdBy = ResolveUser(Request.Form["createdBy"].ToString());

            try
            {
                var result = await _service.UploadPayrollDataAsync(file, category, createdBy);
                if (result.Success)
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        rowsProcessed = result.RowsProcessed,
                        rowsFailed = result.RowsFailed,
                        issues = result.Issues
                    });
                else
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        issues = result.Issues
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculatePayroll(
            [FromQuery] string category = "CL",
            [FromQuery] string? user = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.CalculatePayrollAsync(cat, ResolveUser(user));
                if (result.Success)
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        rowsProcessed = result.RowsProcessed
                    });
                else
                    return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Calculation failed: {ex.Message}");
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitPayroll(
            [FromQuery] string category = "CL",
            [FromQuery] string? user = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.SubmitPayrollAsync(cat, ResolveUser(user));
                if (result.Success)
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        rowsProcessed = result.RowsProcessed,
                        transactionId = result.TransactionId,
                        version = result.Version,
                    });
                else
                    return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Submit failed: {ex.Message}");
            }
        }

        [HttpGet("checker")]
        public async Task<IActionResult> GetCheckerData([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetPayrollCheckerDataAsync(cat);
            return Ok(result);
        }

        [HttpGet("checker/{id}")]
        public async Task<IActionResult> GetCheckerDetail([FromRoute] int id, [FromQuery] string empId, [FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetPayrollCheckerDetailAsync(id, empId, cat);
            if (result == null)
                return NotFound("Employee not found for review");

            return Ok(result);
        }

        [HttpPut("checker/status")]
        public async Task<IActionResult> UpdateCheckerStatus([FromBody] PayrollCheckerStatusUpdateDTO dto)
        {
            if (dto == null)
                return BadRequest("Request body is required");

            // CheckedBy empty ithe SYSTEM ani vestham (null kaadu)
            dto.CheckedBy = ResolveUser(dto.CheckedBy);

            try
            {
                var result = await _service.UpdateCheckerStatusAsync(dto);
                if (result.Success)
                    return Ok(new { success = true, message = result.Message });
                else
                    return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Update failed: {ex.Message}");
            }
        }

        [HttpPost("resign")]
        public async Task<IActionResult> RaiseResignQuery([FromBody] PayrollResignRequestDTO dto)
        {
            if (dto == null)
                return BadRequest("Request body is required");

            var cat = (dto.Category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");
            dto.Category = cat;

            try
            {
                var result = await _service.RaiseResignQueryAsync(dto);
                if (result.Success)
                    return Ok(new { success = true, message = result.Message });
                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed: {ex.Message}");
            }
        }

        [HttpGet("periods")]
        public async Task<IActionResult> GetPeriods([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.GetPayrollPeriodsAsync(cat);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to load periods: {ex.Message}");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string category = "CL",
            [FromQuery(Name = "from")] string fromDate = "",
            [FromQuery(Name = "to")] string toDate = "")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            if (!DateTime.TryParseExact(fromDate?.Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ||
                !DateTime.TryParseExact(toDate?.Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                return BadRequest("from and to must be yyyy-MM-dd");

            try
            {
                var result = await _service.GetPayrollHistoryAsync(cat, f, t);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"History failed: {ex.Message}");
            }
        }

        [HttpGet("live")]
        public async Task<IActionResult> GetLive([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.GetPayrollLiveAttendanceAsync(cat);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Live attendance failed: {ex.Message}");
            }
        }

        [HttpGet("status-summary")]
        public async Task<IActionResult> GetStatusSummary([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            try
            {
                var result = await _service.GetPayrollStatusSummaryAsync(cat);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Status summary failed: {ex.Message}");
            }
        }
    }
}