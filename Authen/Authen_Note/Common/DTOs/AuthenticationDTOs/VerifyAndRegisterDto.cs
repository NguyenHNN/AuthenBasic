using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AuthenticationDTOs
{
    public class VerifyAndRegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string DateOfBirth { get; set; }

        public string Address { get; set; }

        [Required]
        public string Otp { get; set; } // Thêm OTP vào đây
    }
}
