using Repositories;
using Repositories.Models;
using Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private static readonly Dictionary<string, string> _otpStorage = new();

        public UserAccountService(UnitOfWork unitOfWork,IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;

        }
        public async Task<UserAccount?> GetByEmailAsync(string email)
        {

            return await _unitOfWork.UserAccountRepository
                .GetByEmailAsync(email);
        }

        public async Task<bool> CreateAsync(UserAccount userAccount)
        {

            await _unitOfWork.UserAccountRepository
                .CreateAsync(userAccount);

            return true;
        }
    }
}
