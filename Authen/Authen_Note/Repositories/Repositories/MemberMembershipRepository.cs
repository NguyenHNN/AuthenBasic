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
    public class MemberMembershipRepository : GenericRepository<MemberMembership>
    {
        public MemberMembershipRepository()
        {
        }

        public MemberMembershipRepository(SWP391_ChildGrowthTrackingSystemDBContext context)
            => _context = context;
        public async Task<MemberMembership?> GetByIdMemberMembershipAsync(Guid memberMembershipId)
        {
            return await _context.MemberMemberships
                .Include(mm => mm.Member)
                   .ThenInclude(m => m.User) // Include User from Member
                .Include(mm => mm.Package)
                .FirstOrDefaultAsync(mm => mm.MemberMembershipId == memberMembershipId);
        }
        public async Task<List<MemberMembership>> GetAllOldByMemberIdAsync(Guid memberId)
        {
            var memberMemberships = await _context.MemberMemberships
                .Where(mm => mm.MemberId == memberId && mm.Status == "Active")
                .ToListAsync();

            return memberMemberships;
        }
    }
}
