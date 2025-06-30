using Repositories.Base;
using Repositories.DBContext;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>
    {
        public TransactionRepository() { }  
        public TransactionRepository (SWP391_ChildGrowthTrackingSystemDBContext context) => _context = context;
        public async Task<Transaction?> GetByGatewayTransactionIdAsync(long gatewayTransactionId)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.MemberMembership)
                       .ThenInclude(t => t.Package)         // Include Package from MemberMembership
                .FirstOrDefaultAsync(t => t.GatewayTransactionId == gatewayTransactionId);

        }
    }
}
