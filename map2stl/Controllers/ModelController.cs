using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModelController : ControllerBase
    {
        public class BoundingBoxRequest
        {
            public double SouthLat { get; set; }
            public double WestLng { get; set; }
            public double NorthLat { get; set; }
            public double EastLng { get; set; }
        }

        // POST: generate
        [HttpPost("generateStl")]
        public IActionResult GenerateStl([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            // TODO: Generate an STL from these coordinates.
            byte[] stlBytes = GenerateStlFromBoundingBox(request);

            return File(stlBytes, "application/octet-stream", "terrain.stl");
        }

        // This method would call DEM.Net or your own logic
        private byte[] GenerateStlFromBoundingBox(BoundingBoxRequest bbox)
        {
            // 1. Convert the bounding box to whichever format your code needs.
            // 2. Use DEM.Net or your existing code to create an STL model.
            // 3. Return the final STL bytes.

            // For demonstration, just return a dummy byte array:
            return new byte[] { 0x53, 0x54, 0x4C }; // ASCII for "STL"
        }

        [HttpPost("generateGltf")]
        public IActionResult GenerateGltf([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            try
            {
                // Generate the glTF model from the bounding box coordinates
                byte[] gltfBytes = GenerateGltfFromBoundingBox(request);

                // Save the glTF file to disk (optional, for debugging)
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "terrain_model.gltf");
                System.IO.File.WriteAllBytes(filePath, gltfBytes);

                return File(gltfBytes, "model/gltf+json", "terrain_model.gltf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        private byte[] GenerateGltfFromBoundingBox(BoundingBoxRequest bbox)
        {
            // For demonstration purposes, return a dummy byte array
            // Replace this with actual logic for creating a GIF or STL model
            Console.WriteLine($"Generating GltF for BoundingBox: " +
                              $"SouthLat={bbox.SouthLat}, WestLng={bbox.WestLng}, NorthLat={bbox.NorthLat}, EastLng={bbox.EastLng}");
            return Encoding.ASCII.GetBytes("test"); // Simulated GIF header
        }



    }
}
