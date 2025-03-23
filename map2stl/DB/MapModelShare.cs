using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace map2stl.DB
{
    public class MapModelShare
    {
        [Key]
        public int Id { get; set; }

        // The model being shared
        public int MapModelId { get; set; }
        [ForeignKey("MapModelId")]
        public MapModel Model { get; set; } = default!;

        // The user who can access the model
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = default!;

        // Optional permissions
        public bool CanEdit { get; set; } = false;

        public DateTime SharedAt { get; set; }

    }
}
