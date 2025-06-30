using Common.DTOs.GoogleAuthenticationDTOs;
using Common.Enum.UserAccountEnums;
using NanoidDotNet;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Mappers
{
    public static class GoogleAuthenMapper
    {
        public static UserAccount MapToGoogleUser(string email, string name, string? profileUrl, int roleId, byte[]? profilePicture = null)
        {
            return new UserAccount
            {
                Username = email.Split('@')[0],  // Lấy phần trước @ làm username
                Email = email,
                Name = name,
                PhoneNumber = Nanoid.Generate(size: 16), // Không có phone từ Google
                Gender = "Other",  // Google không cung cấp giới tính
                DateOfBirth = null, // Không có DOB từ Google
                Address = "N/A",
                Password = Guid.NewGuid().ToString(), // Dummy password
                ProfilePicture = profilePicture,
                Status = UserAccountStatus.Active.ToString(),
                RoleId = 1,
                RegistrationDate = DateTime.UtcNow,
                IsVerified = true,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
