using map2stl.DB;
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
            // 1. Check if username or email is taken using FirstOrDefaultAsync
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "Username already exists." });

            // 2. Validate inputs
            if (request.Password == null || request.Username == null || request.Email == null)
            {
                return BadRequest(new { message = "Bad request: null value" });
            }

            // 3. Hash password (salt is generated and stored with the hash)
            string passwordHash = HashPassword(request.Password);

            // 4. Create user
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = UserRole.User // Default role
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
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials (username)" });

            if (request.Password == null)
            {
                return BadRequest(new { message = "Bad request: null value" });
            }

            // 2. Verify password using the stored hash (which contains the salt)
            if (!VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials (password)" });

            // 3. Generate JWT
            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        /// <summary>
        /// Hashes the password using a randomly generated salt.
        /// The returned string is a Base64-encoded concatenation of salt and hash.
        /// </summary>
        private string HashPassword(string password)
        {
            // Generate a random salt (16 bytes)
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Convert password to bytes and combine with salt
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

            // Hash the salted password using SHA256 (32 bytes output)
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(saltedPassword);

            // Combine salt + hash and return as Base64 string
            byte[] saltAndHash = new byte[salt.Length + hashBytes.Length];
            Buffer.BlockCopy(salt, 0, saltAndHash, 0, salt.Length);
            Buffer.BlockCopy(hashBytes, 0, saltAndHash, salt.Length, hashBytes.Length);

            return Convert.ToBase64String(saltAndHash);
        }

        /// <summary>
        /// Verifies the given password by extracting the salt from the stored hash,
        /// recomputing the hash using the provided password and the same salt, and comparing the results.
        /// </summary>
        private bool VerifyPassword(string password, string storedSaltAndHash)
        {
            // Decode the stored Base64 string to get the salt and hash bytes
            byte[] saltAndHashBytes = Convert.FromBase64String(storedSaltAndHash);

            // Assuming the salt is 16 bytes and SHA256 produces a 32-byte hash:
            const int saltSize = 16;
            if (saltAndHashBytes.Length < saltSize)
            {
                return false; // Invalid stored hash format
            }

            byte[] salt = new byte[saltSize];
            Buffer.BlockCopy(saltAndHashBytes, 0, salt, 0, saltSize);

            // Extract the stored hash bytes
            int hashSize = saltAndHashBytes.Length - saltSize;
            byte[] storedHashBytes = new byte[hashSize];
            Buffer.BlockCopy(saltAndHashBytes, saltSize, storedHashBytes, 0, hashSize);

            // Convert the provided password to bytes and combine with the extracted salt
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

            // Compute the hash for the provided password
            using var sha256 = SHA256.Create();
            byte[] computedHashBytes = sha256.ComputeHash(saltedPassword);

            // Compare computed hash with the stored hash
            return computedHashBytes.SequenceEqual(storedHashBytes);
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _config["JwtSettings:SecretKey"]; // Read from appsettings
            if (secretKey == null)
            {
                throw new Exception("Secret key not found.");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("username", user.Username),
                new Claim("role", user.Role.ToString()),
                //new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("sub", user.Id.ToString())
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

        // DTOs 
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
