using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollApproverController : ControllerBase
    {
        private readonly IPayrollApproverService _service;

        public PayrollApproverController(IPayrollApproverService service)
        {
            _service = service;
        }

        // 👈 NEW: Logged-in user name: frontend value first, then auth identity, else SYSTEM (same as PayrollEmployeeController)
        private string ResolveUser(string? fromClient)
        {
            if (!string.IsNullOrWhiteSpace(fromClient)) return fromClient.Trim();
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
                return User.Identity.Name!;
            return "SYSTEM";
        }

        // GET api/PayrollApprover/approver?category=CL
        [HttpGet("approver")]
        public async Task<IActionResult> GetApproverData([FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetPayrollApproverDataAsync(cat);
            return Ok(result);
        }

        // GET api/PayrollApprover/approver/{id}?empId=..&category=CL
        [HttpGet("approver/{id}")]
        public async Task<IActionResult> GetApproverDetail([FromRoute] int id, [FromQuery] string empId, [FromQuery] string category = "CL")
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");

            var result = await _service.GetPayrollApproverDetailAsync(id, empId, cat);
            if (result == null)
                return NotFound("Employee not found for approval");

            return Ok(result);
        }

        // PUT api/PayrollApprover/approver/status  (single OR bulk – UI calls it once per record)
        [HttpPut("approver/status")]
        public async Task<IActionResult> UpdateApproverStatus([FromBody] PayrollApproverStatusUpdateDTO dto)
        {
            if (dto == null)
                return BadRequest("Request body is required");

            var cat = (dto.Category ?? "CL").Trim().ToUpperInvariant();
            if (cat != "CL" && cat != "NAPS")
                return BadRequest("category must be CL or NAPS");
            dto.Category = cat;

            // 👈 NEW: ApprovedBy empty ithe null kaakunda auth identity / SYSTEM ani vestham
            dto.ApprovedBy = ResolveUser(dto.ApprovedBy);

            try
            {
                var result = await _service.UpdateApproverStatusAsync(dto);
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
    }
}