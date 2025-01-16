using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config; 

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // 1. Check if username or email is taken
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username already exists." });

            // 2. Hash password
            if (request.Password == null || request.Username == null || request.Email == null) 
            { 
                return BadRequest(new { message =  "bad rekvest null value" });
            }
            string passwordHash = HashPassword(request.Password);

            // 3. Create user
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // 1. Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null) return Unauthorized(new { message = "Invalid credentials(name)" });

            if (request.Password == null)
            {
                return BadRequest(new { message = "bad rekvest null value" });
            }
            // 2. Verify password
            var hash = HashPassword(request.Password);
            if (user.PasswordHash != hash)
                return Unauthorized(new { message = "Invalid credentials(pass)" });

            // 3. Generate JWT
            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _config["JwtSettings:SecretKey"]; // Read from appsettings
            if(secretKey == null)
            {
                throw new Exception("Secret key not found.");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                //new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("username", user.Username),
                new Claim("role", user.Role.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("id",user.Id.ToString())

            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //  DTOs 
        public class RegisterRequest
        {
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        public class LoginRequest
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }
    }
}
