using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace map2stl.DB
{
    public class MapDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MapModelId { get; set; }

        [ForeignKey("MapModelId")]
        public MapModel MapModel { get; set; }

        [Required]
        public double TopLeftLatitude { get; set; }

        [Required]
        public double TopLeftLongitude { get; set; }

        [Required]
        public double BottomRightLatitude { get; set; }

        [Required]
        public double BottomRightLongitude { get; set; }

        [Required]
        public double ZFactor { get; set; }
    }
}
