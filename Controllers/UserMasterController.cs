using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.UserMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserMasterController : ControllerBase
    {
        private readonly IUserMasterService _service;

        public UserMasterController(IUserMasterService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var result = await _service.GetDepartmentsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = "User not found" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserMasterDTO dto)
        {
            var (success, message, id) = await _service.CreateAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { id, message });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateUserMasterDTO dto)
        {
            var (success, message) = await _service.UpdateAsync(id, dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { message });
            return Ok(new { message });
        }

        // 👇 NEW
        [HttpGet("privileges/{userId}")]
        public async Task<IActionResult> GetPrivileges(int userId)
        {
            var result = await _service.GetPrivilegesAsync(userId);
            return Ok(result);
        }

        [HttpPost("privileges")]
        public async Task<IActionResult> SavePrivileges([FromBody] List<SaveUserPrivilegeDTO> dto)
        {
            var (success, message) = await _service.SavePrivilegesAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}