using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt; // Needed for decoding JWT
using static map2stl.Controllers.ModelController;
using map2stl.DB;

namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModelController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public ModelController(AppDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        public class BoundingBoxRequest
        {
            public double SouthLat { get; set; }
            public double WestLng { get; set; }
            public double NorthLat { get; set; }
            public double EastLng { get; set; }
        }

        // POST: generateStl
        [HttpPost("generateStl")]
        public IActionResult GenerateStl([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            byte[] stlBytes = GenerateStlFromBoundingBox(request);
            return File(stlBytes, "application/octet-stream", "terrain.stl");
        }

        private byte[] GenerateStlFromBoundingBox(BoundingBoxRequest bbox)
        {
            // Dummy STL file for now, replace with actual STL generation
            return new byte[] { 0x53, 0x54, 0x4C }; // ASCII for "STL"
        }




    //POST: generateGltf
    [HttpPost("generateGltf")]
    public async Task<IActionResult> GenerateGltf([FromBody] BoundingBoxRequest request)
    {
        if (request == null)
            return BadRequest("No bounding box provided.");

        try
        {
            // Construct the API request URL
            string apiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/" +
                            $"{request.WestLng},{request.EastLng},{request.SouthLat},{request.NorthLat}" +
                            $"?dataset=SRTM_GL3&textured=true&imageryProvider=MapBox-SatelliteStreet" +
                            $"&textureQuality=2&format=glTF&zFactor=1&adornments=false&meshReduceFactor=0.69" +
                            $"&clientConnectionId=7I6FTpIxL0qIuNJaY3hHTA&onlyEstimateSize=false";

            // Fetch the JSON response from the API
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Failed to fetch glTF metadata: {errorContent}");
            }

            // Parse JSON response
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;

            // Extract the model file path
            string modelFilePath = root.GetProperty("assetInfo").GetProperty("modelFile").GetString();
            string fullModelUrl = $"https://api.elevationapi.com{modelFilePath}"; // Construct full download URL

            // Download the .glb file
            HttpResponseMessage modelResponse = await _httpClient.GetAsync(fullModelUrl);
            if (!modelResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)modelResponse.StatusCode, "Failed to download glTF model file.");
            }

            byte[] glbBytes = await modelResponse.Content.ReadAsByteArrayAsync();

            // 🏗️ 1. Create directory structure (models/YYYY-MM-DD/HH-MM)
            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "models");
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            //string timeFolder = DateTime.UtcNow.ToString("HH-mm");

            // Check if user is authenticated and get user ID (JWT)
            string userId = "guest"; // Default
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                string token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value; // "sub" is the standard claim for User ID

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    userId = userIdClaim; // Use user ID if available
                }
            }
            else
            {
                // Generate a random session ID if user is not logged in
                userId = $"session_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // Create the full directory path
            string fullDirectory = Path.Combine(basePath, dateFolder);
            Directory.CreateDirectory(fullDirectory); // Ensure the directory exists

            // 📝 2. Define the final file name (UserID_UUID.glb)
            string fileName = $"{userId}_{Guid.NewGuid().ToString().Substring(0, 8)}.glb";
            string finalFilePath = Path.Combine(fullDirectory, fileName);

            // 💾 3. Save the file
            await System.IO.File.WriteAllBytesAsync(finalFilePath, glbBytes);

            // ✅ 4. Return the file URL instead of the file itself
            string fileUrl = $"/models/{dateFolder}/{fileName}";

            return Ok(new { success = true, fileUrl = fileUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }



    // GET: userModels
    [HttpGet("userModels")]
        public IActionResult GetUserModels()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null)
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim.Value, out var userId))
                return BadRequest("Invalid User ID.");

            var models = _context.Models.Where(m => m.UserId == userId).ToList();
            return Ok(models);
        }
    }
}
