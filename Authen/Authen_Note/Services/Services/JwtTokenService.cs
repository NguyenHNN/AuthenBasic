using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Repositories.Models;
using Services.IServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        private readonly UnitOfWork _unitOfWork;
        private static readonly ConcurrentDictionary<string, (Guid UserId, DateTime Expiry)> _refreshTokens = new();
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();
        public JwtTokenService(IConfiguration config, UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _config = config;

            // Kiểm tra cấu hình JWT
            if (string.IsNullOrEmpty(_config["Jwt:Key"]) || string.IsNullOrEmpty(_config["Jwt:Issuer"]) || string.IsNullOrEmpty(_config["Jwt:Audience"]))
            {
                throw new InvalidOperationException("JWT configuration is missing (Key, Issuer, or Audience).");
            }
            var keyBytes = Convert.FromBase64String(_config["Jwt:Key"]);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 bytes long.");
            }
        }
        public string GenerateJSONWebToken(UserAccount userAccount)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var profilePicture = Convert.ToBase64String(userAccount.ProfilePicture ?? new byte[0]);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                new Claim[]
                {
                new(ClaimTypes.Name, userAccount.Name),
                new(ClaimTypes.NameIdentifier, userAccount.UserId.ToString()),
                new(ClaimTypes.Role, userAccount.RoleId.ToString()),
                new(ClaimTypes.Email, userAccount.Email),
                new("ProfileImage", profilePicture),
                new("IsVerified", userAccount.IsVerified.ToString()) // Use a custom claim type for IsVerified
                },
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            if (!_refreshTokens.TryGetValue(refreshToken, out var tokenData) || tokenData.Expiry <= DateTime.UtcNow)
            {
                return default;
            }

            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(tokenData.UserId);
            if (user == null)
            {
                return default;
            }

            // Xóa refresh token cũ
            _refreshTokens.TryRemove(refreshToken, out _);

            // Tạo access token mới
            var newAccessToken = GenerateJSONWebToken(user);

            // Tạo refresh token mới
            var newRefreshToken = GenerateRefreshToken();
            _refreshTokens[newRefreshToken] = (user.UserId, DateTime.UtcNow.AddDays(7));

            return (newAccessToken, newRefreshToken);
        }

        public bool RevokeToken(string token)
        {
            return _revokedTokens.TryAdd(token, DateTime.UtcNow.AddMinutes(10));
        }

        public bool IsTokenRevoked(string token)
        {
            return _revokedTokens.ContainsKey(token) && _revokedTokens[token] > DateTime.UtcNow;
        }

        public void StoreRefreshToken(string refreshToken, Guid userId, DateTime expiry)
        {
            //_refreshTokens[refreshToken] = (userId, expiry);

            // Xóa các refresh token cũ của userId
            var oldTokens = _refreshTokens.Where(t => t.Value.UserId == userId).Select(t => t.Key).ToList();
            foreach (var oldToken in oldTokens)
            {
                _refreshTokens.TryRemove(oldToken, out _);
            }
            _refreshTokens[refreshToken] = (userId, expiry);
        }
        public void RevokeRefreshTokensByUserId(Guid userId)
        {
            var oldTokens = _refreshTokens.Where(t => t.Value.UserId == userId).Select(t => t.Key).ToList();
            foreach (var oldToken in oldTokens)
            {
                _refreshTokens.TryRemove(oldToken, out _);
            }
        }
    }
    }

