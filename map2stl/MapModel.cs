using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace map2stl
{
    public class MapModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        // Byte array to store the model as a BLOB
        [Required]
        public required byte[] Data { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        // link this STL to a specific user  a foreign key:
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? Owner { get; set; }
    }
}