using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface IJwtTokenService
    {
        string GenerateJSONWebToken(UserAccount user);
        string GenerateRefreshToken();
        Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken);
        bool RevokeToken(string token);
        bool IsTokenRevoked(string token);
        void StoreRefreshToken(string refreshToken, Guid userId, DateTime expiry);
        void RevokeRefreshTokensByUserId(Guid userId);


    }
}
