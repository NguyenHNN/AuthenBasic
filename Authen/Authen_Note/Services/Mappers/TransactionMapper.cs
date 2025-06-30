using Repositories.Models;
using Common.Enum.MemberMembershipEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPAY.NET.Models;

namespace Services.Mappers
{
    public static class TransactionMapper
    {
        public static void UpdateTransactionFromVNPayResponse(this Transaction transaction, PaymentResult vnpayResponse)
        {
            transaction.PaymentStatus = vnpayResponse.IsSuccess
                ? Common.Enum.TransactionEnums.TransactionStatus.Completed.ToString()
                : Common.Enum.TransactionEnums.TransactionStatus.Failed.ToString();

            transaction.PaymentDate = DateTime.Now;
            transaction.PaymentMethod = vnpayResponse.PaymentMethod ?? transaction.PaymentMethod;
            transaction.Currency = "VND";
            transaction.TransactionType = "VNpay";

            //Update MemberMembership
            transaction.MemberMembership.StartDate = DateTime.Now;
            transaction.MemberMembership.EndDate = DateTime.Now.AddMonths(transaction.MemberMembership.Package.DurationMonths);

            transaction.MemberMembership.Status = vnpayResponse.IsSuccess
                ? MemberMembershipStatus.Active.ToString()
                : MemberMembershipStatus.Inactive.ToString();
            transaction.MemberMembership.IsActive = vnpayResponse.IsSuccess;
            transaction.MemberMembership.UpdatedAt = DateTime.Now;
        }
    }
}
