using map2stl.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using static map2stl.Controllers.ModelController;
using System.Net.Http;
using System.Text.Json;

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
            // Check if username or email is taken using FirstOrDefaultAsync
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "Username already exists." });

            if (request.Password == null || request.Username == null || request.Email == null)
            {
                return BadRequest(new { message = "Bad request: null value" });
            }

            // Hash password (salt is generated and stored with the hash)
            string passwordHash = HashPassword(request.Password);

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
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials (username)" });

            if (request.Password == null)
                return BadRequest(new { message = "Bad request: null value" });

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials (password)" });

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        /// <summary>
        /// Endpoint for when a user has forgotten their password.
        /// This will send a password reset token via email.
        /// </summary>
        [HttpPost("forgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            // To avoid email enumeration, always return OK even if user is not found.
            if (user == null)
                return Ok(new { message = "If an account with that email exists, a password reset email has been sent." });

            var token = GeneratePasswordResetToken(user);           
            await SendPasswordResetEmail(user.Email, token);

            return Ok(new { message = "If an account with that email exists, a password reset email has been sent." });
        }

        /// <summary>
        /// Endpoint for resetting password with a token (from the forgot password email).
        /// </summary>
        [HttpPost("resetPasswordWithToken")]
        public async Task<IActionResult> ResetPasswordWithToken(ResetPasswordWithTokenRequest request)
        {
            var principal = GetPrincipalFromToken(request.Token);
            if (principal == null)
                return BadRequest(new { message = "Invalid or expired token." });

            // Check that the token has the correct purpose.
            if (principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value != "pwdReset")
                return BadRequest(new { message = "Invalid token." });

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null)
                return BadRequest(new { message = "Invalid token." });

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.PasswordHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }

        // --------------------------
        // Helper Methods & DTOs
        // --------------------------

        /// <summary>
        /// Hashes the password using a randomly generated salt.
        /// Returns a Base64-encoded concatenation of salt and hash.
        /// </summary>
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(saltedPassword);

            byte[] saltAndHash = new byte[salt.Length + hashBytes.Length];
            Buffer.BlockCopy(salt, 0, saltAndHash, 0, salt.Length);
            Buffer.BlockCopy(hashBytes, 0, saltAndHash, salt.Length, hashBytes.Length);

            return Convert.ToBase64String(saltAndHash);
        }

        /// <summary>
        /// Verifies the provided password by extracting the salt from the stored hash,
        /// recomputing the hash, and comparing the results.
        /// </summary>
        private bool VerifyPassword(string password, string storedSaltAndHash)
        {
            byte[] saltAndHashBytes = Convert.FromBase64String(storedSaltAndHash);
            const int saltSize = 16;
            if (saltAndHashBytes.Length < saltSize)
                return false;

            byte[] salt = new byte[saltSize];
            Buffer.BlockCopy(saltAndHashBytes, 0, salt, 0, saltSize);

            int hashSize = saltAndHashBytes.Length - saltSize;
            byte[] storedHashBytes = new byte[hashSize];
            Buffer.BlockCopy(saltAndHashBytes, saltSize, storedHashBytes, 0, hashSize);

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

            using var sha256 = SHA256.Create();
            byte[] computedHashBytes = sha256.ComputeHash(saltedPassword);

            return computedHashBytes.SequenceEqual(storedHashBytes);
        }

        /// <summary>
        /// Generates a standard JWT for authentication.
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var secretKey = _config["JwtSettings:SecretKey"];
            if (secretKey == null)
                throw new Exception("Secret key not found.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("username", user.Username),
                new Claim("role", user.Role.ToString()),
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

        /// <summary>
        /// Generates a JWT token specifically for password reset.
        /// The token is short-lived (e.g. 15 minutes) and includes a purpose claim.
        /// </summary>
        private string GeneratePasswordResetToken(User user)
        {
            var secretKey = _config["JwtSettings:SecretKey"];
            if (secretKey == null)
                throw new Exception("Secret key not found.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("purpose", "pwdReset")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validates the given token (used for password reset) and returns the principal.
        /// </summary>
        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var secretKey = _config["JwtSettings:SecretKey"];
            if (secretKey == null)
                throw new Exception("Secret key not found.");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true, // Token must be unexpired
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;
                if (jwtSecurityToken == null ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Placeholder for email sending logic.
        /// Implement your email service here, reading any necessary configuration from appsettings.json.
        /// </summary>
        private async Task SendPasswordResetEmail(string email, string token)
        {
            var resetUrl = $"http://localhost:3000/resetpassword?token={token}";
            var subject = "Password Reset Request";
            var body = $"Click the link below to reset your password:\n{resetUrl}";

            // Read SMTP settings from configuration
            var smtpHost = _config["Email:Smtp:Host"];
            var smtpPort = int.Parse(_config["Email:Smtp:Port"] ?? "465");
            var smtpUser = _config["Email:Smtp:User"];
            var smtpPass = _config["Email:Smtp:Password"];
            var fromEmail = _config["Email:From"];

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(fromEmail);
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    // Make sure to disable default credentials before setting your own
                    client.UseDefaultCredentials = false;

                    // For Websupport, SSL is typically required
                    client.EnableSsl = true;

                    // Supply your login credentials
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);

                    await client.SendMailAsync(message);
                }
            }
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

        public class ForgotPasswordRequest
        {
            public string? Email { get; set; }
        }

        public class ResetPasswordWithTokenRequest
        {
            public string Token { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
        }
    }
}

