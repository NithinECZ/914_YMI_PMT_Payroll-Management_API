using Microsoft.AspNetCore.Mvc;
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

        // GET api/PayrollEmployee?category=CL      -> active CL employees
        // GET api/PayrollEmployee?category=NAPS    -> active NAPS employees
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetPayrollEmployeesAsync(cat);
            return Ok(result);
        }

        // POST api/PayrollEmployee/upload
        // Accepts: form-data with "file" and "category"
        // Returns: { success: true/false, message: string, rowsProcessed: int, rowsFailed: int }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPayrollData()
        {
            var file = Request.Form.Files.FirstOrDefault();
            var category = Request.Form["category"].ToString();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (string.IsNullOrWhiteSpace(category) || (category != "CL" && category != "NAPS"))
                return BadRequest("Invalid or missing category (must be CL or NAPS)");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                return BadRequest("File must be .csv, .xlsx, or .xls");

            try
            {
                var result = await _service.UploadPayrollDataAsync(file, category);
                if (result.Success)
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        rowsProcessed = result.RowsProcessed,
                        rowsFailed = result.RowsFailed
                    });
                else
                    return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }
    }
}