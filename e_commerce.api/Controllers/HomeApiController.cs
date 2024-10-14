using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace e_commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeApiController : ControllerBase
    {
        private readonly ILogger<HomeApiController> _logger;

        public HomeApiController(ILogger<HomeApiController> logger)
        {
            _logger = logger;
        }

        [HttpGet("index")]
        [Authorize] // This will allow any authenticated user
        public IActionResult Index()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return Ok(new { 
                message = "Welcome to the home page!",
                userRole = userRole
            });
        }

        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            return Ok(new { Message = "This is the privacy policy content." });
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            return StatusCode(500, new { 
                Message = "An error occurred.", 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}