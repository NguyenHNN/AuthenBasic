using Common.DTOs.AuthenticationDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Repositories.Models;
using Services.IServices;
using Services.Mappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class AuthenService : IAuthenService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();
        private static readonly ConcurrentDictionary<string, bool> _verifiedEmails = new();
        private static readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _resetTokens = new();
        // Tập ký tự cho các trường hợp OTP
        private static readonly string _digits = "0123456789"; // Chỉ số
        private static readonly string _uppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Chữ cái in hoa
        private static readonly string _lowercaseLetters = "abcdefghijklmnopqrstuvwxyz"; // Chữ cái in thường
        private static readonly string _allLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"; // In hoa và in thường
        private static readonly string _uppercaseAndDigits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // In hoa và số
        private static readonly string _lowercaseAndDigits = "abcdefghijklmnopqrstuvwxyz0123456789"; // In thường và số
        private static readonly string _allCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // In hoa, in thường, số

        public AuthenService(UnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<bool> SendOtpAsync(string email)
        {
            // Kiểm tra xem email đã tồn tại chưa
            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
            if (user != null) return false; // Email đã tồn tại, trả về false

            Random random = new Random();
            string otp;
            char[] otpChars = new char[6];

            // Chọn ngẫu nhiên một trong 7 trường hợp OTP
            int caseSelector = random.Next(0, 7);
            string characterSet = caseSelector switch
            {
                0 => _digits,                    // Trường hợp 1: Chỉ số (e.g., 483920)
                1 => _uppercaseLetters,         // Trường hợp 2: Chỉ chữ cái in hoa (e.g., KXYPQR)
                2 => _lowercaseLetters,         // Trường hợp 3: Chỉ chữ cái in thường (e.g., pxkqwe)
                3 => _allLetters,               // Trường hợp 4: In hoa và in thường (e.g., KjPqRt)
                4 => _uppercaseAndDigits,       // Trường hợp 5: In hoa và số (e.g., K7N4P8)
                5 => _lowercaseAndDigits,       // Trường hợp 6: In thường và số (e.g., k9p3m2)
                6 => _allCharacters,            // Trường hợp 7: In hoa, in thường, số (e.g., Kx9Pq2)
                _ => _allCharacters             // Dự phòng, mặc định dùng tất cả
            };

            // Tạo OTP: Chọn ngẫu nhiên 6 ký tự từ characterSet
            for (int i = 0; i < 6; i++)
            {
                otpChars[i] = characterSet[random.Next(characterSet.Length)];
            }
            otp = new string(otpChars);

            // Lưu OTP với thời hạn 5 phút
            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(5));

            // Chuẩn bị email chứa OTP
            string subject = "[BabyHaven] Account Registration OTP";
            string body = $"<html><body style='font-family:Arial, sans-serif; color:#333; text-align:center;'>"
                        + "<div style='max-width:600px; margin:auto; padding:20px; border-radius:10px; background:#f9f9f9; box-shadow:0 0 10px rgba(0,0,0,0.1);'>"
                        + "<img src='https://i.pinimg.com/736x/5a/99/6a/5a996a8df2a8ea9452ea11da2160df80.jpg' alt='BabyHaven Logo' style='width:120px; height:120px; border-radius:50%; margin-bottom:20px;'>"
                        + "<h2 style='color:#00d0bc;'>Welcome to BabyHaven!</h2>"
                        + "<p>Thank you for registering an account at <b>BabyHaven</b>. Please use the OTP below to complete your registration:</p>"
                        + "<h3 style='color:#00d0bc; font-size:24px; background:#e0f7f5; display:inline-block; padding:10px 20px; border-radius:5px;'><b>" + otp + "</b></h3>"
                        + "<p><i>Note: This OTP is valid for 5 minutes. Do not share it with anyone.</i></p>"
                        + "<br><p>Best regards,<br><b>BabyHaven Team</b></p>"
                        + "</div></body></html>";

            // Gửi email chứa OTP
            await _emailService.SendEmailAsync(email, subject, body);
            return true;
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            // Xác minh OTP: Kiểm tra OTP và thời hạn
            if (_otpStorage.TryGetValue(email, out var storedOtp) && storedOtp.Otp == otp && storedOtp.Expiry > DateTime.UtcNow)
            {
                _otpStorage.Remove(email, out _);
                return true;
            }
            return false;
        }

        //public async Task<UserAccount> Authenticate(string email)
        //{
        //    // Xác thực người dùng bằng email và mật khẩu
        //    return await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
        //}
        public async Task<UserAccount?> Authenticate(string email, string password)
        {
            // Lấy tài khoản hợp lệ (Status = Active, IsVerified = true)
            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            // Xác minh mật khẩu
            var passwordHasher = new PasswordHasher<string>();
            var verificationResult = passwordHasher.VerifyHashedPassword(null, user.Password, password);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                return null;
            }

            return user;
        }

        //public async Task<bool> SendResetPasswordOtpAsync(string email)
        //{
        //    var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
        //    if (user == null) return false;

        //    string otp = new Random().Next(100000, 999999).ToString();
        //    _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(5));

        //    string subject = "[BabyHaven] Password Reset OTP";
        //    string body = $"<html><body style='font-family:Arial, sans-serif; color:#333; text-align:center;'>"
        //                + "<div style='max-width:600px; margin:auto; padding:20px; border-radius:10px; background:#f9f9f9; box-shadow:0 0 10px rgba(0,0,0,0.1);'>"
        //                + "<img src='https://i.pinimg.com/736x/5a/99/6a/5a996a8df2a8ea9452ea11da2160df80.jpg' alt='BabyHaven Logo' style='width:120px; height:120px; border-radius:50%; margin-bottom:20px;'>"
        //                + "<h2 style='color:#00d0bc;'>Hello,</h2>"
        //                + "<p>We received a request to reset your password. Please use the OTP below to proceed:</p>"
        //                + "<h3 style='color:#00d0bc; font-size:24px; background:#e0f7f5; display:inline-block; padding:10px 20px; border-radius:5px;'><b>" + otp + "</b></h3>"
        //                + "<p><i>Note: This OTP is valid for 5 minutes. If you did not request a password reset, please ignore this email.</i></p>"
        //                + "<br><p>Best regards,<br><b>BabyHaven Team</b></p>"
        //                + "</div></body></html>";
        //    await _emailService.SendEmailAsync(email, subject, body);
        //    return true;
        //}

        public async Task<bool> SendResetPasswordOtpAsync(string email)
        {
            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
            if (user == null) return false;

            // Kiểm tra xem có OTP còn hiệu lực không
            if (_otpStorage.TryGetValue(email, out var storedOtp) && storedOtp.Expiry > DateTime.UtcNow)
            {
                return false; // OTP cũ còn hiệu lực, không gửi OTP mới
            }

            Random random = new Random();
            string otp;
            char[] otpChars = new char[6];

            // Chọn ngẫu nhiên một trong 7 trường hợp OTP
            int caseSelector = random.Next(0, 7);
            string characterSet = caseSelector switch
            {
                0 => _digits,                    // Chỉ số (e.g., 483920)
                1 => _uppercaseLetters,         // Chữ cái in hoa (e.g., KXYPQR)
                2 => _lowercaseLetters,         // Chữ cái in thường (e.g., pxkqwe)
                3 => _allLetters,               // In hoa và in thường (e.g., KjPqRt)
                4 => _uppercaseAndDigits,       // In hoa và số (e.g., K7N4P8)
                5 => _lowercaseAndDigits,       // In thường và số (e.g., k9p3m2)
                6 => _allCharacters,            // In hoa, in thường, số (e.g., Kx9Pq2)
                _ => _allCharacters             // Dự phòng, mặc định dùng tất cả
            };

            // Tạo OTP: Chọn ngẫu nhiên 6 ký tự từ characterSet
            for (int i = 0; i < 6; i++)
            {
                otpChars[i] = characterSet[random.Next(characterSet.Length)];
            }
            otp = new string(otpChars);

            // Lưu OTP với thời hạn 5 phút
            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(5));

            // Chuẩn bị email chứa OTP
            string subject = "[BabyHaven] Password Reset OTP";
            string body = $"<html><body style='font-family:Arial, sans-serif; color:#333; text-align:center;'>"
                        + "<div style='max-width:600px; margin:auto; padding:20px; border-radius:10px; background:#f9f9f9; box-shadow:0 0 10px rgba(0,0,0,0.1);'>"
                        + "<img src='https://i.pinimg.com/736x/5a/99/6a/5a996a8df2a8ea9452ea11da2160df80.jpg' alt='BabyHaven Logo' style='width:120px; height:120px; border-radius:50%; margin-bottom:20px;'>"
                        + "<h2 style='color:#00d0bc;'>Hello,</h2>"
                        + "<p>We received a request to reset your password. Please use the OTP below to proceed:</p>"
                        + "<h3 style='color:#00d0bc; font-size:24px; background:#e0f7f5; display:inline-block; padding:10px 20px; border-radius:5px;'><b>" + otp + "</b></h3>"
                        + "<p><i>Note: This OTP is valid for 5 minutes. If you did not request a password reset, please ignore this email.</i></p>"
                        + "<br><p>Best regards,<br><b>BabyHaven Team</b></p>"
                        + "</div></body></html>";
            await _emailService.SendEmailAsync(email, subject, body);
            return true;
        }


        //public async Task<bool> VerifyResetPasswordOtpAsync(string email, string otp)
        //{
        //    return _otpStorage.TryGetValue(email, out var storedOtp)
        //        && storedOtp.Otp == otp
        //        && storedOtp.Expiry > DateTime.UtcNow;
        //}

        //public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        //{
        //    var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
        //    if (user == null) return false;

        //    user.Password = newPassword;
        //    await _unitOfWork.UserAccountRepository.UpdateAsync(user);
        //    _otpStorage.Remove(email, out _);

        //    return true;
        //}


        //public async Task<bool> IsOtpVerified(string email)
        //{
        //    return _verifiedEmails.TryGetValue(email, out var isVerified) && isVerified;
        //}

        //public async Task MarkOtpVerified(string email)
        //{
        //    _verifiedEmails[email] = true;
        //}
        //// Các phương thức mới để hỗ trợ token
        public async Task<(bool Success, string ResetToken)> VerifyResetPasswordOtpWithTokenAsync(string email, string otp)
        {
            // Kiểm tra tài khoản tồn tại và hợp lệ
            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return (false, null); // Tài khoản không tồn tại hoặc không hợp lệ
            }
            if (_otpStorage.TryGetValue(email, out var storedOtp) && storedOtp.Otp == otp && storedOtp.Expiry > DateTime.UtcNow)
            {
                _otpStorage.Remove(email, out _);
                _verifiedEmails[email] = true;

                // Tạo reset token
                string resetToken = Guid.NewGuid().ToString();
                _resetTokens[resetToken] = (email, DateTime.UtcNow.AddMinutes(10)); // Token hết hạn sau 10 phút
                return (true, resetToken);
            }
            return (false, null);
        }

        public async Task<bool> ResetPasswordWithTokenAsync(string resetToken, string newPassword)
        {
            if (_resetTokens.TryGetValue(resetToken, out var tokenData) && tokenData.Expiry > DateTime.UtcNow)
            {
                var email = tokenData.Email;
                var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
                if (user == null) return false;

                // Hash mật khẩu mới
                var passwordHasher = new PasswordHasher<string>();
                string hashedPassword = passwordHasher.HashPassword(null, newPassword);

                // Cập nhật mật khẩu đã hash vào cơ sở dữ liệu
                user.Password = hashedPassword;
                await _unitOfWork.UserAccountRepository.UpdateAsync(user);


                // Gửi email xác nhận
                await _emailService.SendEmailAsync(email, "[BabyHaven] Password Reset Confirmation",
                    "Hello,\nYour password has been successfully reset. If you did not perform this action, please contact support immediately.\n\nBest regards,\nBabyHaven Team");


                // Xóa token và trạng thái xác thực
                _resetTokens.TryRemove(resetToken, out _);
                _verifiedEmails.TryRemove(email, out _);
                return true;
            }
            return false;
        }

        public async Task<bool> SendWelcomeEmailAsync(string email)
        {
            string subject = "[BabyHaven] Welcome to BabyHaven!";
            string body = $"Hello,\nThank you for joining BabyHaven! Your account has been successfully created. Start exploring our services to track your child's growth.\n\nBest regards,\nBabyHaven Team";
            return await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}


//using Common.DTOs.AuthenticationDTOs;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using Repositories;
//using Repositories.Models;
//using Services.IServices;
//using Services.Mappers;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Security.Claims;
//using System.Text;
//using System.Threading.Tasks;

//namespace Services.Services
//{
//    public class AuthenService : IAuthenService
//    {
//        private readonly UnitOfWork _unitOfWork;
//        private readonly IEmailService _emailService;
//        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();
//        private static readonly ConcurrentDictionary<string, bool> _verifiedEmails = new();
//        private static readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _resetTokens = new();
//        private static readonly string _characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // Ký tự cho OTP

//        public AuthenService(UnitOfWork unitOfWork, IEmailService emailService)
//        {
//            _unitOfWork = unitOfWork;
//            _emailService = emailService;
//        }

//        public async Task<bool> SendOtpAsync(string email)
//        {
//            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
//            if (user != null) return false; // Email đã tồn tại

//            // Tạo OTP 6 ký tự ngẫu nhiên (kết hợp chữ và số)
//            Random random = new Random();
//            char[] otpChars = new char[6];
//            for (int i = 0; i < 6; i++)
//            {
//                otpChars[i] = _characters[random.Next(_characters.Length)];
//            }
//            string otp = new string(otpChars);

//            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(5));

//            string subject = "[BabyHaven] Account Registration OTP";
//            string body = $"<html><body style='font-family:Arial, sans-serif; color:#333; text-align:center;'>"
//                        + "<div style='max-width:600px; margin:auto; padding:20px; border-radius:10px; background:#f9f9f9; box-shadow:0 0 10px rgba(0,0,0,0.1);'>"
//                        + "<img src='https://i.pinimg.com/736x/5a/99/6a/5a996a8df2a8ea9452ea11da2160df80.jpg' alt='BabyHaven Logo' style='width:120px; height:120px; border-radius:50%; margin-bottom:20px;'>"
//                        + "<h2 style='color:#00d0bc;'>Welcome to BabyHaven!</h2>"
//                        + "<p>Thank you for registering an account at <b>BabyHaven</b>. Please use the OTP below to complete your registration:</p>"
//                        + "<h3 style='color:#00d0bc; font-size:24px; background:#e0f7f5; display:inline-block; padding:10px 20px; border-radius:5px;'><b>" + otp + "</b></h3>"
//                        + "<p><i>Note: This OTP is valid for 5 minutes. Do not share it with anyone.</i></p>"
//                        + "<br><p>Best regards,<br><b>BabyHaven Team</b></p>"
//                        + "</div></body></html>";

//            await _emailService.SendEmailAsync(email, subject, body);
//            return true;
//        }

//        public async Task<bool> VerifyOtpAsync(string email, string otp)
//        {
//            if (_otpStorage.TryGetValue(email, out var storedOtp) && storedOtp.Otp == otp && storedOtp.Expiry > DateTime.UtcNow)
//            {
//                _otpStorage.Remove(email, out _);
//                return true;
//            }
//            return false;
//        }

//        public async Task<UserAccount> Authenticate(string email, string password)
//        {
//            return await _unitOfWork.UserAccountRepository.GetUserAccount(email, password);
//        }
//    }
//}








//using Common.DTOs.AuthenticationDTOs;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using Repositories;
//using Repositories.Models;
//using Services.IServices;
//using Services.Mappers;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Security.Claims;
//using System.Text;
//using System.Threading.Tasks;


//namespace Services.Services
//{
//    public class AuthenService: IAuthenService
//    {
//        private readonly UnitOfWork _unitOfWork;
//        private readonly IEmailService _emailService;
//        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();
//        private static readonly ConcurrentDictionary<string, bool> _verifiedEmails = new();
//        private static readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _resetTokens = new(); // Lưu trữ token -> email
//        public AuthenService(UnitOfWork unitOfWork, IEmailService emailService)
//        {
//            _unitOfWork = unitOfWork;
//            _emailService = emailService;
//        }

//        public async Task<bool> SendOtpAsync(string email)
//        {
//            var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(email);
//            if (user != null) return false; // Email đã tồn tại

//            string otp = new Random().Next(100000, 999999).ToString();
//            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(5));

//            string subject = "[BabyHaven] Account Registration OTP";
//            string body = $"<html><body style='font-family:Arial, sans-serif; color:#333; text-align:center;'>"
//                        + "<div style='max-width:600px; margin:auto; padding:20px; border-radius:10px; background:#f9f9f9; box-shadow:0 0 10px rgba(0,0,0,0.1);'>"
//                        + "<img src='https://i.pinimg.com/736x/5a/99/6a/5a996a8df2a8ea9452ea11da2160df80.jpg' alt='BabyHaven Logo' style='width:120px; height:120px; border-radius:50%; margin-bottom:20px;'>"
//                        + "<h2 style='color:#00d0bc;'>Welcome to BabyHaven!</h2>"
//                        + "<p>Thank you for registering an account at <b>BabyHaven</b>. Please use the OTP below to complete your registration:</p>"
//                        + "<h3 style='color:#00d0bc; font-size:24px; background:#e0f7f5; display:inline-block; padding:10px 20px; border-radius:5px;'><b>" + otp + "</b></h3>"
//                        + "<p><i>Note: This OTP is valid for 5 minutes. Do not share it with anyone.</i></p>"
//                        + "<br><p>Best regards,<br><b>BabyHaven Team</b></p>"
//                        + "</div></body></html>";

//            await _emailService.SendEmailAsync(email, subject, body);
//            return true;
//        }

//        public async Task<bool> VerifyOtpAsync(string email, string otp)
//        {
//            if (_otpStorage.TryGetValue(email, out var storedOtp) && storedOtp.Otp == otp && storedOtp.Expiry > DateTime.UtcNow)
//            {
//                _otpStorage.Remove(email, out _);
//                return true;
//            }
//            return false;
//        }

//        public async Task<UserAccount> Authenticate(string email, string password)
//        {
//            return await _unitOfWork.UserAccountRepository.GetUserAccount(email, password);
//        }
//    }
//}
