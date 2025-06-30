using Common.DTOs.GoogleAuthenticationDTOs;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Server;
using Repositories;
using Repositories.Models;
using Repositories.Repositories;
using Services.Base;
using Services.IServices;
using Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class GoogleAuthenService:IGoogleAuthenService
    {
        private readonly IConfiguration _config;
        private readonly UnitOfWork _unitOfWork;
        private readonly IUserAccountService _userAccountService;
        private readonly IJwtTokenService _jwtTokenService;

        public GoogleAuthenService(IConfiguration config,UnitOfWork unitOfWork, IUserAccountService userAccountService, IJwtTokenService jwtTokenService)
        {
            _config = config;
            _unitOfWork = unitOfWork;
            _userAccountService = userAccountService;
            _jwtTokenService = jwtTokenService;
        }
        public async Task<ServiceResult> LoginWithGoogleAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                if (payload == null || !payload.EmailVerified)
                {
                    return new ServiceResult(401, "ID Token không hợp lệ hoặc email chưa xác minh.");
                }

                //var email = payload.Email;
                //var name = payload.Name;
                //var profileUrl = payload.Picture;
                //var roleId = 1;

                var user = await _unitOfWork.UserAccountRepository.GetByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = GoogleAuthenMapper.MapToGoogleUser(payload.Email, payload.Name, payload.Picture, 1, null);
                    await _unitOfWork.UserAccountRepository.CreateAsync(user);
                    await _unitOfWork.UserAccountRepository.SaveAsync();
                }

                var token = _jwtTokenService.GenerateJSONWebToken(user);

                var response = new GoogleLoginResponse
                {
                    Token = token,
                    Email = user.Email,
                    FullName = user.Name,
                    ProfilePictureUrl = payload.Picture,
                    RoleId = 1
                };

                return new ServiceResult(200, "Đăng nhập thành công", response);
            }
            catch (InvalidJwtException ex)
            {
                return new ServiceResult(400, "Token không hợp lệ", new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return new ServiceResult(500, "Lỗi hệ thống", new List<string> { ex.Message });
            }
        }


    }
}
