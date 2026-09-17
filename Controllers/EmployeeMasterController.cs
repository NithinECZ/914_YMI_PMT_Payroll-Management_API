using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.EmployeeMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeMasterController : ControllerBase
    {
        private readonly IEmployeeMasterService _service;

        public EmployeeMasterController(IEmployeeMasterService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("genders")]
        public async Task<IActionResult> GetGenders()
        {
            return Ok(await _service.GetGendersAsync());
        }

        [HttpGet("empcategories")]
        public async Task<IActionResult> GetEmpCategories()
        {
            return Ok(await _service.GetEmpCategoriesAsync());
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            return Ok(await _service.GetDepartmentsAsync());
        }

        [HttpGet("subdivisions")]
        public async Task<IActionResult> GetSubDivisions()
        {
            return Ok(await _service.GetSubDivisionsAsync());
        }

        [HttpGet("skillcategories")]
        public async Task<IActionResult> GetSkillCategories()
        {
            return Ok(await _service.GetSkillCategoriesAsync());
        }

        [HttpGet("maritalstatus")]
        public async Task<IActionResult> GetMaritalStatus()
        {
            return Ok(await _service.GetMaritalStatusAsync());
        }

        [HttpGet("vendors")]
        public async Task<IActionResult> GetVendors()
        {
            return Ok(await _service.GetVendorsAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = "Employee not found" });
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateEmployeeMasterDTO dto)
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
    }
}