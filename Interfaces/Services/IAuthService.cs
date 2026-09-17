using YMI_PMT_PayrollManagement_API.DTOs.Auth;
using System.Threading.Tasks;

namespace YMI_PMT_PayrollManagement_API.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
        (bool IsValid, string ErrorMessage) ValidateLoginRequest(LoginRequestDTO request);
    }
}