using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
//using System.IdentityModel.Tokens.Jwt; // Needed for decoding JWT
using static map2stl.Controllers.ModelController;
using map2stl.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModelController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

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
            public double zFactor { get; set; }

            public double meshReduceFactor { get; set; }
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




        [HttpPost("generateGltf")]
        public async Task<IActionResult> GenerateGltf([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            try
            {
                // 1. Construct the API request URL
                string apiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/" +
                                $"{request.WestLng},{request.EastLng},{request.SouthLat},{request.NorthLat}" +
                                $"?dataset=SRTM_GL3&textured=true&imageryProvider=MapBox-SatelliteStreet" +
                                $"&textureQuality=2&format=glTF&zFactor=1&adornments=false&meshReduceFactor=0.69" +
                                $"&clientConnectionId=7I6FTpIxL0qIuNJaY3hHTA&onlyEstimateSize=false";

                // 2. Fetch the JSON response from the external API
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Failed to fetch glTF metadata: {errorContent}");
                }

                // 3. Parse JSON response to extract the model file path
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                var root = jsonDoc.RootElement;
                string modelFilePath = root.GetProperty("assetInfo").GetProperty("modelFile").GetString();
                string fullModelUrl = $"https://api.elevationapi.com{modelFilePath}"; // Full download URL

                // 4. Download the .glb file
                HttpResponseMessage modelResponse = await _httpClient.GetAsync(fullModelUrl);
                if (!modelResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)modelResponse.StatusCode, "Failed to download glTF model file.");
                }
                byte[] glbBytes = await modelResponse.Content.ReadAsByteArrayAsync();

                // 5. Check if the user is authenticated by looking for the JWT token
                int userId = 0;
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    string token = authHeader.Substring("Bearer ".Length);
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    // "sub" (or "id") claim holds the user id; adjust based on your token claims
                    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
                    {
                        userId = parsedId;
                    }
                }

                if (userId != 0)
                {
                    var mapModel = new MapModel
                    {
                        Name = $"Model_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                        GLBData = glbBytes, // Store the downloaded glbBytes
                        Description = "Generated from bounding box",
                        UserId = userId
                    };

                    _context.Models.Add(mapModel);
                    await _context.SaveChangesAsync();

                    // Trigger asynchronous STL conversion using the bounding box (request)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            byte[] stlBytes = await ConvertGlbToStl(request);
                            mapModel.STLData = stlBytes;
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error converting model {mapModel.Id} to STL: {ex.Message}");
                        }
                    });
                }


                // 8. (Optional) Save the GLB file to disk, creating a folder structure by date.
                string basePath = Path.Combine(Directory.GetCurrentDirectory(), "models");
                string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string fullDirectory = Path.Combine(basePath, dateFolder);
                Directory.CreateDirectory(fullDirectory); // Ensure directory exists
                string fileName = $"glb_{Guid.NewGuid().ToString().Substring(0, 8)}.glb";
                string finalFilePath = Path.Combine(fullDirectory, fileName);
                await System.IO.File.WriteAllBytesAsync(finalFilePath, glbBytes);
                string fileUrl = $"/models/{dateFolder}/{fileName}";

                // 9. Return the URL to the stored GLB file
                return Ok(new { success = true, fileUrl = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private async Task<byte[]> ConvertGlbToStl(BoundingBoxRequest bbox)
        {
            // Use your client connection ID or get it from configuration
            string clientId = "2hRsNholWShDxDtfpRM6Qg";

            // Construct the STL API request URL using the bounding box parameters.
            string apiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/" +
                            $"{bbox.WestLng},{bbox.EastLng},{bbox.SouthLat},{bbox.NorthLat}" +
                            $"?dataset=SRTM_GL3&textured=true&imageryProvider=MapBox-SatelliteStreet" +
                            $"&textureQuality=2&format=STL&zFactor=3&adornments=false&meshReduceFactor=1" +
                            $"&clientConnectionId={clientId}&onlyEstimateSize=false";

            // Send the request to the external API
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch STL file from external API.");
            }
            // Read and return the STL bytes
            return await response.Content.ReadAsByteArrayAsync();
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

        [Authorize]
        [HttpGet("shareModel/{modelId}")]
        public async Task<IActionResult> ShareModel(int modelId)
        {
            // Validate that the model belongs to the authenticated user.
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null)
                return Unauthorized("User not found in token.");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Invalid user id.");

            var model = await _context.Models.FirstOrDefaultAsync(m => m.Id == modelId && m.UserId == userId);
            if (model == null)
                return NotFound("Model not found.");

            // Generate a share token (JWT) with a purpose claim.
            var secretKey = _config["JwtSettings:SecretKey"];
            if (secretKey == null)
                throw new Exception("Secret key not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("modelId", modelId.ToString()),
                new Claim("purpose", "share")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                signingCredentials: creds
            );
            string shareToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Build the share link.
            // For example, assume you expose an endpoint at GET /sharedModel that accepts a token.
            string shareLink = $"{Request.Scheme}://{Request.Host}/sharedModel?token={shareToken}";
            return Ok(new { shareLink });
        }

        [HttpGet("sharedModel")]
        public async Task<IActionResult> SharedModel([FromQuery] string token, [FromQuery] string format = "glb")
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is required.");

            var secretKey = _config["JwtSettings:SecretKey"];
            if (secretKey == null)
                throw new Exception("Secret key not configured.");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                if (principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value != "share")
                    return BadRequest("Invalid token purpose.");

                // Get modelId from token.
                string modelIdStr = principal.Claims.FirstOrDefault(c => c.Type == "modelId")?.Value;
                if (!int.TryParse(modelIdStr, out int modelId))
                    return BadRequest("Invalid model id in token.");

                // Retrieve the model without checking the owner.
                var model = await _context.Models.FirstOrDefaultAsync(m => m.Id == modelId);
                if (model == null)
                    return NotFound("Model not found.");

                byte[] fileBytes;
                string extension;
                if (format.ToLower() == "stl")
                {
                    fileBytes = model.STLData;
                    extension = "stl";
                }
                else
                {
                    fileBytes = model.GLBData;
                    extension = "glb";
                }

                // Return the file directly as a download.
                return File(fileBytes, "application/octet-stream", $"{model.Name}.{extension}");
            }
            catch
            {
                return BadRequest("Invalid or expired token.");
            }
        }



        [Authorize]
        [HttpGet("downloadModel/{modelId}")]
        public async Task<IActionResult> DownloadModel(int modelId, [FromQuery] string format = "glb")
        {
            // Get the user ID from the JWT claims.
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null)
                return Unauthorized("User not found in token.");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Invalid user id.");

            // Retrieve the model ensuring it belongs to the authenticated user.
            var model = await _context.Models.FirstOrDefaultAsync(m => m.Id == modelId && m.UserId == userId);
            if (model == null)
                return NotFound("Model not found.");

            byte[] fileBytes;
            string extension;
            if (format.ToLower() == "stl")
            {
                fileBytes = model.STLData;
                extension = "stl";
            }
            else // default to GLB
            {
                fileBytes = model.GLBData;
                extension = "glb";
            }

            // Return the file directly as a download.
            return File(fileBytes, "application/octet-stream", $"{model.Name}.{extension}");
        }


    }
}
