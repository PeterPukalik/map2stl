using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

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

        // Self-reference for versioning: 
        // If this model is a version, ParentId holds the Id of its parent model
        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public MapModel? Parent { get; set; }

        // Navigation property for child versions
        public List<MapModel> Versions { get; set; } = new List<MapModel>();

        // Additional properties from BoundingBoxRequest:

        [Required]
        public double SouthLat { get; set; }

        [Required]
        public double WestLng { get; set; }

        [Required]
        public double NorthLat { get; set; }

        [Required]
        public double EastLng { get; set; }

        [Required]
        public double zFactor { get; set; }

        [Required]
        public double meshReduceFactor { get; set; }

        [Required]
        public int estimateSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string format { get; set; }
    }
}
