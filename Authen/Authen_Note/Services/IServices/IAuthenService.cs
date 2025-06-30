using Common.DTOs.AuthenticationDTOs;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface IAuthenService
    {
        Task<UserAccount> Authenticate(string email, string password);
        Task<bool> SendOtpAsync(string email);

        Task<bool> VerifyOtpAsync(string email, string otp);

        Task<bool> SendResetPasswordOtpAsync(string email);

        //Task<bool> VerifyResetPasswordOtpAsync(string email, string otp);

        //Task<bool> ResetPasswordAsync(string email, string newPassword);
        //// Đánh dấu OTP đã xác thực
        //Task MarkOtpVerified(string email);

        //// Kiểm tra trạng thái OTP
        //Task<bool> IsOtpVerified(string email);

        // Phương thức mới
        Task<(bool Success, string ResetToken)> VerifyResetPasswordOtpWithTokenAsync(string email, string otp);
        Task<bool> ResetPasswordWithTokenAsync(string resetToken, string newPassword);
        Task<bool> SendWelcomeEmailAsync(string email);
    }
}
