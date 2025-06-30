using Common;
using Services.Base;
using Services.IServices;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET;
using Microsoft.AspNetCore.Http;
using Repositories;
using Services.Mappers;
using Common.DTOs.VNPayDTOs;
using Common.Enum.MemberMembershipEnums;

namespace Services.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IVnpay _vnPay;
        private readonly IConfiguration _config;
        private readonly UnitOfWork _unitOfWork;

        public VNPayService(IVnpay vnPay, IConfiguration config, UnitOfWork unitOfWork)
        {
            _vnPay = vnPay;
            _config = config;
            _unitOfWork ??= new UnitOfWork();

            // Khởi tạo cấu hình VNPAY từ appsettings.json
            _vnPay.Initialize(
                _config["Vnpay:TmnCode"],
                _config["Vnpay:HashSecret"],
                _config["Vnpay:BaseUrl"],
                _config["Vnpay:ReturnUrl"]
            );
        }
        public async Task<IServiceResult> CreatePaymentUrl(long gatewayTransactionId, string ipAddress)
        {
            try
            {
                var transaction = await _unitOfWork.TransactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);

                if (transaction == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Transaction not found");
                }

                var membership = await _unitOfWork.MemberMembershipRepository.GetByIdMemberMembershipAsync(transaction.MemberMembershipId);

                if (membership == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Membership not found");
                }

                var request = new PaymentRequest
                {
                    PaymentId = transaction.GatewayTransactionId,
                    Money = Convert.ToDouble(membership.Package.Price),
                    Description = membership.Package.Description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY, // Cho phép chọn bất kỳ ngân hàng nào
                    CreatedDate = DateTime.UtcNow.AddHours(7),
                    Currency = Currency.VND,
                    Language = DisplayLanguage.Vietnamese
                };

                // Tạo URL thanh toán
                var paymentUrl = _vnPay.GetPaymentUrl(request);

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Create payment URL successfully!", paymentUrl);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


        public async Task<IServiceResult> ValidateResponse(IQueryCollection queryParams)
        {
            try
            {
                var paymentResult = _vnPay.GetPaymentResult(queryParams);
                var gatewayTransactionId = paymentResult.PaymentId;

                var transaction = await _unitOfWork.TransactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);

                if (transaction == null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Transaction not found!");
                }

                if (transaction.MemberMembership == null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Membership not found!");
                }



                if (paymentResult.IsSuccess is true)
                {
                    var member = await _unitOfWork.MemberRepository.GetMemberByUserId(transaction.UserId);

                    if (member == null)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Member not found!");
                    }

                    var existingMemberships = await _unitOfWork.MemberMembershipRepository
                        .GetAllOldByMemberIdAsync(member.MemberId);
                    foreach (var membership in existingMemberships)
                    {
                        membership.Status = MemberMembershipStatus.Suspended.ToString();
                        await _unitOfWork.MemberMembershipRepository.UpdateAsync(membership);
                    }
                }

                transaction.UpdateTransactionFromVNPayResponse(paymentResult);
                await _unitOfWork.MemberMembershipRepository.UpdateAsync(transaction.MemberMembership);
                await _unitOfWork.TransactionRepository.UpdateAsync(transaction);

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Payment status: " + transaction.PaymentStatus, transaction);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

    }
}
