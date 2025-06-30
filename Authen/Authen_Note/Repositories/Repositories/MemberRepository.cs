using Microsoft.EntityFrameworkCore;
using Repositories.Base;
using Repositories.DBContext;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class MemberRepository : GenericRepository<Member>
    {
        public MemberRepository()
        {
        }

        public MemberRepository(SWP391_ChildGrowthTrackingSystemDBContext context)
            => _context = context;
        public async Task<Member?> GetMemberByUserId(Guid userId)
        {
            return await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }
    }
}
