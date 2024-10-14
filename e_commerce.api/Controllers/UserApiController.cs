using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using e_commerce.api.Models;
using e_commerce.api.Data;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;


namespace e_commerce.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private readonly IMongoDBRepository<UserRegistration> _userRepository;
        private readonly ILogger<UserApiController> _logger;
        private readonly IConfiguration _configuration;

        public UserApiController(IMongoDBRepository<UserRegistration> userRepository, ILogger<UserApiController> logger, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin model)
        {
            try
            {
                var user = await _userRepository.FindOneAsync(u => u.Username == model.Username);
                if (user != null && VerifyPassword(model.Password, user.Password))
                {
                    var token = GenerateJwtToken(user);
                    return Ok(new { Token = token });
                }
                return Unauthorized(new { Message = "Invalid username or password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred during login for user: {model.Username}");
                return StatusCode(500, new { Message = "An internal server error occurred. Please try again later." });
            }
        }

       [HttpPost("register")]
public async Task<IActionResult> Registration([FromBody] UserRegistration model)
{
    _logger.LogInformation($"Received registration request for: {model.Username}");

    

    // Check if the user already exists
    try
    {
        var existingUser = await _userRepository.FindOneAsync(u => u.Username == model.Username || u.Email == model.Email);
        if (existingUser != null)
        {
            _logger.LogWarning($"User already exists: {model.Username}");
            return Conflict(new { Message = "Username or email already exists." });
        }

        // Hash the password
        model.Password = HashPassword(model.Password);

        // Create a new user
        var newUser = new UserRegistration
        {
            FullName = model.FullName,
            Username = model.Username,
            Email = model.Email,
            Password = model.Password,
            PhoneNumber = model.PhoneNumber,
            UserType = model.UserType
        };

        // Insert the new user into the database
        await _userRepository.InsertOneAsync(newUser);

        _logger.LogInformation($"User registered successfully: {newUser.Username}");
        return Ok(new { Message = "Registration successful.", RedirectTo = "/Home/Index" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error during user registration: {model.Username}. Exception: {ex.Message}");
        return StatusCode(500, new { Message = "An error occurred during registration. Please try again." });
    }
}



        private string GenerateJwtToken(UserRegistration user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }

        
    }
}