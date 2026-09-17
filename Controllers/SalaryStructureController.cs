using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YMI_PMT_PayrollManagement_API.DTOs.SalaryStructure;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalaryStructureController : ControllerBase
    {
        private readonly ISalaryStructureService _service;
        private readonly ILogger<SalaryStructureController> _logger;

        public SalaryStructureController(
            ISalaryStructureService service,
            ILogger<SalaryStructureController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAll salary structures failed");
                return StatusCode(500, new ApiErrorDTO
                {
                    Message = "Could not load salary structures: " + Root(ex)
                });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return NotFound(new ApiErrorDTO { Message = "Salary Structure not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById salary structure {Id} failed", id);
                return StatusCode(500, new ApiErrorDTO
                {
                    Message = "Could not load this salary structure: " + Root(ex)
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSalaryStructureDTO dto)
        {
            if (dto == null)
                return BadRequest(new ApiErrorDTO { Message = "Request body is empty" });

            try
            {
                var (success, message, field, id) = await _service.CreateAsync(dto);

                if (!success)
                {
                    var status = field == "structureName" ? 409 : 400;
                    return StatusCode(status, new ApiErrorDTO { Message = message, Field = field });
                }

                return Ok(new { id, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create salary structure failed");
                return StatusCode(500, new ApiErrorDTO { Message = "Save failed: " + Root(ex) });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSalaryStructureDTO dto)
        {
            if (dto == null)
                return BadRequest(new ApiErrorDTO { Message = "Request body is empty" });

            try
            {
                var (success, message, field) = await _service.UpdateAsync(id, dto);

                if (!success)
                {
                    var status = field == "structureName" ? 409
                               : message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? 404
                               : 400;
                    return StatusCode(status, new ApiErrorDTO { Message = message, Field = field });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update salary structure {Id} failed", id);
                return StatusCode(500, new ApiErrorDTO { Message = "Update failed: " + Root(ex) });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var (success, message) = await _service.DeleteAsync(id);
                if (!success)
                    return NotFound(new ApiErrorDTO { Message = message });

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete salary structure {Id} failed", id);
                return StatusCode(500, new ApiErrorDTO { Message = "Delete failed: " + Root(ex) });
            }
        }

        private static string Root(Exception ex)
        {
            var e = ex;
            while (e.InnerException != null) e = e.InnerException;
            return e.Message;
        }
    }
}