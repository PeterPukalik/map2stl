using map2stl.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                //.Include(u => u.Models)
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("resetPassword/{userId}")]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            var defaultPassword = "NewPassword123"; 
            user.PasswordHash = HashPassword(defaultPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Password reset  for user {user.Username}." });
            //return Ok(new { message = $"Password reset to '{defaultPassword}' for user {user.Username}." });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
