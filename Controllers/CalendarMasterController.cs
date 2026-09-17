using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.CalendarMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarMasterController : ControllerBase
    {
        private readonly ICalendarMasterService _service;
        private readonly ILogger<CalendarMasterController> _logger;

        public CalendarMasterController(ICalendarMasterService service, ILogger<CalendarMasterController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetByMonthYear([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                if (month < 1 || month > 12)
                    return BadRequest(new { message = "Invalid month" });

                var holidays = await _service.GetByMonthYearAsync(month, year);
                return Ok(holidays);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching holidays: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var holiday = await _service.GetByIdAsync(id);
                if (holiday == null)
                    return NotFound(new { message = "Holiday not found" });

                return Ok(holiday);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching holiday: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCalendarMasterDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (success, message, id) = await _service.CreateAsync(dto);
                if (!success)
                    return BadRequest(new { message });

                return Ok(new { id, message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating holiday: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCalendarMasterDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (success, message) = await _service.UpdateAsync(id, dto);
                if (!success)
                    return BadRequest(new { message });

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating holiday: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var (success, message) = await _service.DeleteAsync(id);
                if (!success)
                    return BadRequest(new { message });

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting holiday: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkCreateCalendarDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (success, message) = await _service.BulkCreateAsync(dto);
                if (!success)
                    return BadRequest(new { message });

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error bulk creating holidays: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }
}