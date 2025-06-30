using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.GoogleAuthenticationDTOs
{
    public class GoogleLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        [Required]
        public int RoleId { get; set; }
        
    }
}
