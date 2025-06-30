using Microsoft.EntityFrameworkCore;
using Repositories.Base;
using Repositories.DBContext;
using Repositories.Models;
using static BCrypt.Net.BCrypt;

namespace Repositories.Repositories
{
    public class UserAccountRepository : GenericRepository<UserAccount>
    {
        public UserAccountRepository() { }
        public UserAccountRepository(SWP391_ChildGrowthTrackingSystemDBContext context)
            => _context = context;

        public async Task<UserAccount?> GetByEmailAsync(string email)
        {
            return await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active" && u.IsVerified == true);
        }
        public async Task<UserAccount?> GetByIdAsync(Guid userId)
        {
            return await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "Active" && u.IsVerified == true);
        }
    }
}
