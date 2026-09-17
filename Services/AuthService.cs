using YMI_PMT_PayrollManagement_API.DTOs.Auth;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var response = new LoginResponseDTO();

            var (isValid, errorMessage) = ValidateLoginRequest(request);
            if (!isValid)
            {
                response.Success = false;
                response.Message = errorMessage;
                return response;
            }

            try
            {
                var user = await _userRepository.ValidateUserLoginAsync(
                    request.Username!.Trim(),
                    request.Password!
                );

                if (user != null)
                {
                    if (user.Status == "INACTIVE" || user.Status == "0")
                    {
                        response.Success = false;
                        response.Message = "Invalid user";
                        response.User = null;
                        return response;
                    }

                    response.Success = true;
                    response.Message = "Login Successful";
                    response.User = new UserDataDTO
                    {
                        Id = user.Id,
                        UserId = user.UserId ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        DepartmentName = user.DepartmentName ?? string.Empty,
                        UserType = user.UserType,
                        Status = user.Status ?? "1"
                    };

                    System.Diagnostics.Debug.WriteLine($"[AuthService] Login successful for {request.Username}");
                }
                else
                {
                    bool userExists = await _userRepository.UserExistsAsync(request.Username!.Trim());

                    response.Success = false;
                    response.Message = userExists ? "Password incorrect" : "Username incorrect";
                    response.User = null;

                    System.Diagnostics.Debug.WriteLine($"[AuthService] Login failed - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred during login";
                response.User = null;
                System.Diagnostics.Debug.WriteLine($"[AuthService] LoginAsync Error: {ex.Message}");
            }

            return response;
        }

        public (bool IsValid, string ErrorMessage) ValidateLoginRequest(LoginRequestDTO request)
        {
            if (request == null)
                return (false, "Invalid request");

            if (string.IsNullOrWhiteSpace(request.Username))
                return (false, "Username is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                return (false, "Password is required");

            if (request.Username.Length < 3)
                return (false, "Username must be at least 3 characters");

            if (request.Password.Length < 4)
                return (false, "Password must be at least 4 characters");

            if (request.Password.Contains(" "))
                return (false, "Password cannot contain spaces");

            return (true, "");
        }
    }
}