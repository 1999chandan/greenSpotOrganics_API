using greenSpotApi.Data;
using greenSpotApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace greenSpotApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{UserId}")]
        public async Task<ActionResult<UserDto>> GetUserDetails(long UserId)
        {
            var user = await _context.Users.FindAsync(UserId);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            // Map entity to DTO
            var userDto = new UserDto
            {
                UserId = user.UserId,
                Name = user.Username,
                Email = user.Email
            };

            return Ok(userDto);
        }

        [HttpPost("signup")]
        public async Task<ActionResult<User>> SignupNewUser(RegisterDto request)
        {
            // 1. Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Username is already taken.");
            }

            // 2. Create the User object
            var user = new User
            {
                Username = request.Username,
                Email = request.Email
            };

            // 3. Hash the password securely
            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password); // store string

            // 4. Save to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Verify password using PasswordHasher
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest("Invalid credentials.");
            }

            // Generate JWT Token
            string token = CreateToken(user);
            return Ok(new { token });
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
                return false;

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), passwordHash, password);

            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        // Method to generate JWT for a user
        private string CreateToken(User user)
        {
            // Read configuration from environment variables with defaults
            var secret = System.Environment.GetEnvironmentVariable("JWT_SECRET") 
                         ?? "please_change_this_secret_to_a_secure_value";
            var issuer = System.Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "greenSpotApi";
            var audience = System.Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "greenSpotApiClients";

            // Token lifetime (hours)
            var hoursValid = 1;
            var hoursEnv = System.Environment.GetEnvironmentVariable("JWT_EXPIRES_HOURS");
            if (!string.IsNullOrEmpty(hoursEnv) && int.TryParse(hoursEnv, out var parsedHours))
            {
                hoursValid = parsedHours;
            }

            // Build claims
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username ?? string.Empty),
                new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, System.Guid.NewGuid().ToString())
            };

            // Create signing key and credentials
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var signingCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                securityKey,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            // Token descriptor
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = System.DateTime.UtcNow.AddHours(hoursValid),
                SigningCredentials = signingCredentials,
                Issuer = issuer,
                Audience = audience
            };

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(securityToken);

            return tokenString;
        }
        // Controllers/UserController.cs
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserDto>> GetUserDetailsByEmail(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return NotFound(new { message = $"User with email {email} not found." });

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Name = user.Username,
                Email = user.Email
            };
            return Ok(userDto);
        }

    }
}
