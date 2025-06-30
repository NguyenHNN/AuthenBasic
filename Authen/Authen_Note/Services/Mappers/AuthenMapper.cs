using Common.DTOs.AuthenticationDTOs;
using Repositories.Models;
using System.Text;

namespace Services.Mappers
{
    public static class AuthenMapper
    {
        // Mapper LoginDto
        public static LoginDto MapToLoginDto(this LoginDto dto)
        {
            return new LoginDto
            {
                Email = dto.Email,
                Password = dto.Password
            };
        }

        public static RegisterDto MapToRegisterDto(RegisterDto dto)
        {
            return new RegisterDto
            {
                Email = dto.Email
            };
        }

        public static VerifyAndRegisterDto MapToVerifyAndRegisterDto(VerifyAndRegisterDto dto)
        {

            return new VerifyAndRegisterDto
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = dto.Password,
                PhoneNumber = dto.PhoneNumber,
                FullName = dto.FullName,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                Address = dto.Address,
                Otp = dto.Otp

            };
        }

        public static ResetPasswordDto MapToResetPasswordDto(ResetPasswordDto dto)
        {
            return new ResetPasswordDto
            {
                Email = dto.Email
            };
        }

        public static VerifyOtpForResetDto MapToVerifyOtpForResetDto(VerifyOtpForResetDto dto)
        {
            return new VerifyOtpForResetDto
            {
                Email = dto.Email,
                Otp = dto.Otp
            };
        }

        public static TokenWithResetPasswordDto MapToTokenWithResetPasswordDto(TokenWithResetPasswordDto dto)
        {
            return new TokenWithResetPasswordDto
            {
                ResetToken = dto.ResetToken,
                NewPassword = dto.NewPassword
            };
        }
    }
}
