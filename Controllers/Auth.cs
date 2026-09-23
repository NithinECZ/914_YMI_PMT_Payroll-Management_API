using Microsoft.AspNetCore.Mvc;
using YMI_PMT_PayrollManagement_API.DTOs.Auth;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace YMI_PMT_PayrollManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
       
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponseDTO
                {
                    Success = false,
                    Message = "Invalid request format"
                });
            }

            try
            {
                var result = await _authService.LoginAsync(request);

                if (result.Success)
                    return Ok(result);
                else
                    return Unauthorized(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new LoginResponseDTO
                {
                    Success = false,
                    Message = "An error occurred during login",
                    User = null
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "API is running" });
        }
    }
}