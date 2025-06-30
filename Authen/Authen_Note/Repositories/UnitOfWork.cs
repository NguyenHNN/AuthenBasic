using Repositories.DBContext;
using Repositories.Models;
using Repositories.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class UnitOfWork
    {
        private SWP391_ChildGrowthTrackingSystemDBContext context;
        private UserAccountRepository userAccountRepository;
        private TransactionRepository transactionRepository;
        private MemberMembershipRepository memberMembershipRepository;
        private MemberRepository memberRepository;
        public UnitOfWork()
        {
            context ??= new SWP391_ChildGrowthTrackingSystemDBContext();
        }
        public UserAccountRepository UserAccountRepository
        {
            get
            {
                return userAccountRepository ??= new UserAccountRepository(context);
            }
        }
        public TransactionRepository TransactionRepository
        {
            get
            {
                return transactionRepository ??= new TransactionRepository(context);
            }
        }
        public MemberMembershipRepository MemberMembershipRepository
        {
            get
            {
                return memberMembershipRepository ??= new MemberMembershipRepository(context);
            }
        }
        public MemberRepository MemberRepository
        {
            get
            {
                return memberRepository ??= new MemberRepository(context);
            }
        }
    }
}
