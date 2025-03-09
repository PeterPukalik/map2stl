using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace map2stl.DB
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Username { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Email { get; set; }

        // Store a hashed password
        [Required]
        [MaxLength(500)]
        public required string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public List<MapModel> Models { get; set; } = new List<MapModel>();

    }
}