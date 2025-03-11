using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace map2stl.DB
{
    public class MapModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        // Master GLB file for viewing
        [Required]
        [Column(TypeName = "BLOB")]
        public required byte[] GLBData { get; set; }

        // Optional STL file for download (after conversion)
        [Column(TypeName = "BLOB")]
        public byte[]? STLData { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        // Link this model to a specific user (foreign key)
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? Owner { get; set; }
    }
}