using Microsoft.AspNetCore.Mvc;

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
        [HttpPost("generate")]
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
    }
}
