using Common.DTOs.GoogleAuthenticationDTOs;
using Microsoft.AspNetCore.Mvc;
using Services.IServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoogleAuthenticationController : ControllerBase
    {
        private readonly IGoogleAuthenService _googleAuthenService;

        public GoogleAuthenticationController(IGoogleAuthenService googleAuthenService)
        {
            _googleAuthenService = googleAuthenService;
        }

        [HttpPost("LoginByGoogle")]
        public async Task<IActionResult> LoginByGoogle([FromBody] GoogleLoginRequest request)
        {
            Console.WriteLine(">> API /loginbygoogle gọi đến. Token: " + request.IdToken);

            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest("Thiếu idToken từ FE");

            var result = await _googleAuthenService.LoginWithGoogleAsync(request.IdToken);

            Console.WriteLine(">> Trả về từ service: " + result.Status + " - " + result.Message);

            if (result.Status == 200)
                return Ok(result);
            else
                return StatusCode(result.Status, result);
        }
    }
}
