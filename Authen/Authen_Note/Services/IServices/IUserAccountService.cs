using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface IUserAccountService
    {
        Task<UserAccount?> GetByEmailAsync(string email);
        Task<bool> CreateAsync(UserAccount userAccount);
    }
}
