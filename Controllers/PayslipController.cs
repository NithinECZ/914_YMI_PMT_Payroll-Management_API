using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.Payslip;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayslipController : ControllerBase
    {
        private readonly IPayslipService _service;

        public PayslipController(IPayslipService service)
        {
            _service = service;
        }

        // GET api/Payslip/periods?category=CL
        [HttpGet("periods")]
        public async Task<IActionResult> GetApprovedPeriods([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetApprovedPeriodsAsync(cat);
            return Ok(result);
        }

        // GET api/Payslip/approved?category=CL&fromDate=2026-07-25&toDate=2026-08-26
        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedData(
            [FromQuery] string category = "CL",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetApprovedPayslipDataAsync(cat, fromDate, toDate);
            return Ok(result);
        }

        // GET api/Payslip/provisions?category=CL
        [HttpGet("provisions")]
        public async Task<IActionResult> GetProvisions([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetProvisionsAsync(cat);
            return Ok(result);
        }

        // POST api/Payslip/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePayslips([FromBody] GeneratePayslipRequestDTO dto)
        {
            if (dto == null || dto.Employees == null || dto.Employees.Count == 0)
                return BadRequest("At least one employee must be selected for payslip generation");

            var result = await _service.GeneratePayslipsAsync(dto);
            if (result.Success)
                return Ok(result);

            return BadRequest(result.Message);
        }

        // POST api/Payslip/send-vendor-emails
        [HttpPost("send-vendor-emails")]
        public async Task<IActionResult> SendVendorEmails([FromBody] SendVendorEmailRequestDTO dto)
        {
            if (dto == null)
                return BadRequest("Request cannot be null");

            var result = await _service.SendVendorEmailsAsync(dto);
            return Ok(result);
        }

        // GET api/Payslip/email-status?category=CL&fromDate=2026-07-25&toDate=2026-08-26
        [HttpGet("email-status")]
        public async Task<IActionResult> GetEmailStatus(
            [FromQuery] string category = "CL",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var list = await _service.GetPayslipEmailStatusAsync(cat, fromDate, toDate);
            return Ok(list);
        }
    }
}
