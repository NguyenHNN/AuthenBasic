using Microsoft.AspNetCore.Http;
using Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface IVNPayService
    {
        Task<IServiceResult> CreatePaymentUrl(long gatewayTransactionId, string ipAddress);

        Task<IServiceResult> ValidateResponse(IQueryCollection parans);
    }
}
