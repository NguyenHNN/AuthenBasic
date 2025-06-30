using Common;
using Common.DTOs.VNPayDTOs;
using Repositories.Models;
using Services.Base;
using Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayController : ControllerBase
    {
        private readonly IVNPayService _vnPayService;
        private readonly IJwtTokenService _jwtTokenService;

        public VNPayController(IVNPayService vnPayService, IJwtTokenService jwtTokenService)
        {
            _vnPayService = vnPayService;
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet("create-payment")]
        public async Task<IActionResult> CreatePaymentUrl(long gatewayTransactionId)
        {
            string ipAddress = NetworkHelper.GetIpAddress(HttpContext);
            var result = await _vnPayService.CreatePaymentUrl(gatewayTransactionId, ipAddress);
            return StatusCode(result.Status, new { message = result.Message, data = result.Data });
        }

        [HttpGet("payment-confirm")]
        public async Task<IActionResult> PaymentConfirm()
        {
            try
            {
                var result = await _vnPayService.ValidateResponse(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("PaymentConfirm Error: " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
