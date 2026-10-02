using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.Report;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollReportController : ControllerBase
    {
        private readonly IPayrollReportService _service;

        public PayrollReportController(IPayrollReportService service)
        {
            _service = service;
        }

        // GET api/PayrollReport?category=CL&fromDate=2026-07-25&toDate=2026-08-26&empId=...&skillCat=...&vendor=...
        [HttpGet]
        public async Task<IActionResult> GetPayrollReport([FromQuery] PayrollReportFilterDTO filter)
        {
            try
            {
                var result = await _service.GetPayrollReportAsync(filter ?? new PayrollReportFilterDTO());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching payroll report: " + ex.Message });
            }
        }

        // POST api/PayrollReport/filter
        [HttpPost("filter")]
        public async Task<IActionResult> FilterPayrollReport([FromBody] PayrollReportFilterDTO filter)
        {
            try
            {
                var result = await _service.GetPayrollReportAsync(filter ?? new PayrollReportFilterDTO());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching payroll report: " + ex.Message });
            }
        }

        // GET api/PayrollReport/filter-options?category=CL
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions([FromQuery] string category = "CL")
        {
            try
            {
                var result = await _service.GetFilterOptionsAsync(category);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching filter options: " + ex.Message });
            }
        }
    }
}
