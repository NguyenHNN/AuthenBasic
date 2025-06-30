using Common;
using Repositories.Models;
using Services.Base;
using Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Common.DTOs.AuthenticationDTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Services.Mappers;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserAccountService _userAccountsService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuthenService _authService;

        public AuthenticationController(IConfiguration config, IJwtTokenService jwtTokenService, IAuthenService authService, IUserAccountService userAccountService)
        {
            _userAccountsService = userAccountService;
            _config = config;
            _jwtTokenService = jwtTokenService;
            _authService = authService;
        }

        [HttpPost("Login")]
        public async Task<IServiceResult> Login([FromBody] LoginDto request)
        {
            // Gọi Authenticate với cả email và password
            var user = await _authService.Authenticate(request.Email, request.Password);

            if (user == null)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid email, password, or account not active/verified.");
            }

            var accessToken = _jwtTokenService.GenerateJSONWebToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Lưu refresh token
            _jwtTokenService.StoreRefreshToken(refreshToken, user.UserId, DateTime.UtcNow.AddDays(7));

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Login successful", new { AccessToken = accessToken, RefreshToken = refreshToken });
        }


        [HttpPost("Logout")]
        public async Task<IServiceResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtTokenService.IsTokenRevoked(token))
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Token already revoked.");
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid user.");
            }

            // Thu hồi access token
            _jwtTokenService.RevokeToken(token);
            _jwtTokenService.RevokeRefreshTokensByUserId(userId);

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Logout successful.");
        }

        [HttpPost("RefreshToken")]
        public async Task<IServiceResult> RefreshToken([FromBody] RefreshTokenDto request)
        {
            var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
            if (result == default)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid or expired refresh token.");
            }
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Token refreshed successfully", new { AccessToken = result.AccessToken, RefreshToken = result.RefreshToken });
            //try
            //{
            //    var (accessToken, refreshToken) = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
            //    return new ServiceResult(Const.SUCCESS_READ_CODE, "Token refreshed successfully", new { AccessToken = accessToken, RefreshToken = refreshToken });
            //}
            //catch (SecurityTokenException ex)
            //{
            //    return new ServiceResult(Const.ERROR_VALIDATION_CODE, ex.Message);
            //}
        }

        [HttpPost("SendOtp")]
        public async Task<IServiceResult> SendOtp([FromBody] RegisterDto request)
        {
            // Kiểm tra email đã tồn tại
            var existingUser = await _userAccountsService.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Email already exists.");
            }

            // Gửi OTP
            var otpSent = await _authService.SendOtpAsync(request.Email);
            if (!otpSent)
            {
                return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Failed to send.");
            }

            // Trả về kết quả thành công
            return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE, "OTP sent. Please verify your email.");
        }


        [HttpPost("Register")]
        public async Task<IServiceResult> Register([FromBody] VerifyAndRegisterDto request)
        {
            // Kiểm tra email đã tồn tại
            var existingUser = await _userAccountsService.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Email already exists.");
            }
            // Xác thực OTP
            var isValid = await _authService.VerifyOtpAsync(request.Email, request.Otp);
            if (!isValid)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid OTP.");
            }

            // Hash mật khẩu
            var passwordHasher = new PasswordHasher<string>();
            var hashedPassword = passwordHasher.HashPassword(null, request.Password);

            // Map request to a UserAccount model
            var userAccount = new UserAccount
            {
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Name = request.FullName,
                Gender = request.Gender,
                DateOfBirth = DateOnly.Parse(request.DateOfBirth),
                Address = request.Address,
                Password = hashedPassword, // Lưu mật khẩu đã hash
                RegistrationDate = DateTime.UtcNow,
                Status = "Active",
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RoleId = 1
            };


            // Lưu vào cơ sở dữ liệu
            var result = await _userAccountsService.CreateAsync(userAccount);

            if (result)
            {
                // Lấy thông tin người dùng đã tạo thành công
                var createdUser = await _userAccountsService.GetByEmailAsync(request.Email);
                if (createdUser != null)
                {
                    await _authService.SendWelcomeEmailAsync(request.Email);
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, "User registered successfully.", new
                    {
                        userId = createdUser.UserId
                    });
                }
            }

            return new ServiceResult(Const.FAIL_CREATE_CODE, "Failed to register user.");
        }


        [HttpPost("ForgetPassword")]
        public async Task<IServiceResult> ForgetPassword([FromBody] ResetPasswordDto request)
        {
            var user = await _userAccountsService.GetByEmailAsync(request.Email);

            if (user == null)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Email not found.");
            }

            var otpSent = await _authService.SendResetPasswordOtpAsync(request.Email);

            if (!otpSent)
            {
                return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Failed to send OTP.");
            }

            return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE, "OTP sent to email.");
        }

        [HttpPost("VerifyResetPasswordOtp")]
        public async Task<IServiceResult> VerifyResetPasswordOtp([FromBody] VerifyOtpForResetDto request)
        {
            var (isValid, resetToken) = await _authService.VerifyResetPasswordOtpWithTokenAsync(request.Email, request.Otp);

            if (!isValid)
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid OTP.");
            }

            return new ServiceResult(Const.SUCCESS_VERIFY_OTP_CODE, "OTP verified successfully.", new { ResetToken = resetToken });
        }

        [HttpPost("ResetPassword")]
        public async Task<IServiceResult> ResetPassword([FromBody] TokenWithResetPasswordDto request)
        {
            var result = await _authService.ResetPasswordWithTokenAsync(request.ResetToken, request.NewPassword);

            if (result)
            {
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Password reset successfully.");
            }

            return new ServiceResult(Const.FAIL_UPDATE_CODE, "Invalid or expired reset token.");
        }

    }
}
