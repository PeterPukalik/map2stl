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
using map2stl.ModelTweaks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;


namespace map2stl.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModelController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IHubContext<ProgressHub> _hubContext;

        public ModelController(AppDbContext context, HttpClient httpClient, IHubContext<ProgressHub> hubContext)
        {
            _context = context;
            _httpClient = httpClient;
            _hubContext = hubContext;
        }

        public class BoundingBoxRequest
        {
            public double SouthLat { get; set; }
            public double WestLng { get; set; }
            public double NorthLat { get; set; }
            public double EastLng { get; set; }
            public double zFactor { get; set; }

            public double meshReduceFactor { get; set; }

            public int estimateSize { get; set; }

            public string format { get; set; }
            public int quality { get; set; }
        }

        public class StlGenerationRequest
        {
            public bool IncludeBuildings { get; set; } = true;
            public double? ZFactor { get; set; }
            public double? MeshReduceFactor { get; set; }
            public double? BasePlateHeightFraction { get; set; }
            public double? BasePlateOffset { get; set; }

            public int quality { get; set; }
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
                                $"&textureQuality=2&format=glTF&zFactor={request.zFactor}" +
                                $"&adornments=false&meshReduceFactor={request.meshReduceFactor}" +
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
                        GLBData = glbBytes,
                        Description = "Generated from bounding box",
                        UserId = userId,
                        SouthLat = request.SouthLat,
                        WestLng = request.WestLng,
                        NorthLat = request.NorthLat,
                        EastLng = request.EastLng,
                        zFactor = request.zFactor,
                        meshReduceFactor = request.meshReduceFactor,
                        estimateSize = request.estimateSize,
                        format = request.format
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


        [HttpPost("generateModel")]
        public async Task<IActionResult> GenerateModel([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            try
            {
                // 1. Construct the API request URL
                string apiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/" +
                                $"{request.WestLng},{request.EastLng},{request.SouthLat},{request.NorthLat}" +
                                $"?dataset=SRTM_GL3&textured=true&imageryProvider=MapBox-SatelliteStreet" +
                                $"&textureQuality=2&format={request.format}&zFactor={request.zFactor}" +
                                $"&adornments=false&meshReduceFactor={request.meshReduceFactor}" +
                                $"&clientConnectionId=7I6FTpIxL0qIuNJaY3hHTA&onlyEstimateSize={(request.estimateSize == 0 ? "false" : "true")}";

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
                        GLBData = glbBytes,
                        Description = "Generated from bounding box",
                        UserId = userId,
                        SouthLat = request.SouthLat,
                        WestLng = request.WestLng,
                        NorthLat = request.NorthLat,
                        EastLng = request.EastLng,
                        zFactor = request.zFactor,
                        meshReduceFactor = request.meshReduceFactor,
                        estimateSize = request.estimateSize,
                        format = request.format
                    };

                    _context.Models.Add(mapModel);
                    await _context.SaveChangesAsync();
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

        [HttpPost("estimateSize")]
        public async Task<IActionResult> EstimateSize([FromBody] BoundingBoxRequest request)
        {
            if (request == null)
                return BadRequest("No bounding box provided.");

            try
            {
                // 1. Construct the API request URL
                string apiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/" +
                                $"{request.WestLng},{request.EastLng},{request.SouthLat},{request.NorthLat}" +
                                $"?dataset=SRTM_GL3&textured=true&imageryProvider=MapBox-SatelliteStreet" +
                                $"&textureQuality=2&format=glTF&zFactor={request.zFactor}" +
                                $"&adornments=false&meshReduceFactor={request.meshReduceFactor}" +
                                $"&clientConnectionId=2hRsNholWShDxDtfpRM6Qg&onlyEstimateSize={(request.estimateSize == 0 ? "false" : "true")}";

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

                double estimatedSize = root.GetProperty("estimatedModelFileSizeMB").GetDouble();

                // Return only the estimatedModelFileSizeMB parameter to the frontend.
                return Ok(new { success = true, estimatedModelFileSizeMB = estimatedSize });

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




        [HttpGet("userModels")]
        public IActionResult GetUserModels()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null)
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim.Value, out var userId))
                return BadRequest("Invalid User ID.");

            // Fetch only the root models (ParentId == null) and project required properties.
            var models = _context.Models
                .Where(m => m.UserId == userId && m.ParentId == null)
                .Include(m => m.Versions)
                .Select(m => new
                {
                    Id = m.Id,
                    Name = m.Name,
                    Versions = m.Versions.Select(v => new
                    {
                        Id = v.Id,
                        Name = v.Name
                    }).ToList()
                })
                .ToList();

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
                if (model.STLData == null || model.STLData.Length == 0)
                {
                    return BadRequest("No STL data available for this model.");
                }
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

        // UPDATED ENDPOINT for debugging - Saves GLB from DB and OSM (as GLB) from API locally
        [HttpPost("create-stl-from-model/{modelId}")]
        //[Authorize] // Temporarily remove auth for easier debugging if needed
        public async Task<IActionResult> CreateStlFromModel(
            int modelId,
            [FromQuery] string connectionId, // Keep for SignalR
            [FromBody] StlGenerationRequest req) // Keep request body
        {
            // Enhanced logging for connectionId at the start
            if (string.IsNullOrEmpty(connectionId))
            {
                Console.WriteLine("Warning: Connection ID not provided for SignalR progress updates.");
            }
            else
            {
                Console.WriteLine($"Received request for model {modelId} with SignalR connectionId: {connectionId}");
            }

            // Function to safely send SignalR messages with enhanced logging
            async Task SendProgress(string message)
            {
                if (!string.IsNullOrEmpty(connectionId))
                {
                    try
                    {
                        // Log before attempting to send
                        Console.WriteLine($"Attempting to send SignalR message to connectionId '{connectionId}': \"{message}\"");
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", message);
                        // Log after successful send attempt (doesn't guarantee receipt, but confirms backend sent it)
                        Console.WriteLine($"Successfully invoked SendAsync for connectionId '{connectionId}'.");
                    }
                    catch (Exception ex)
                    {
                        // Log the specific exception
                        Console.WriteLine($"ERROR sending SignalR message to connectionId '{connectionId}': {ex.GetType().Name} - {ex.Message}");
                        // Optionally log the stack trace for deeper debugging: Console.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    // Log locally if no connection ID was provided
                    Console.WriteLine($"Progress (no connectionId): {message}");
                }
            }

            await SendProgress($"Starting local file saving process for model ID: {modelId}...");

            // 1. Look up the model from the database
            MapModel model; // Declare model variable here
            try
            {
                model = await _context.Models.FirstOrDefaultAsync(m => m.Id == modelId);
            }
            catch (Exception dbEx)
            {
                await SendProgress($"Error accessing database: {dbEx.Message}");
                Console.WriteLine($"Database Error looking up model {modelId}: {dbEx}");
                return StatusCode(500, $"Database error looking up model: {dbEx.Message}");
            }

            if (model == null)
            {
                await SendProgress($"Error: Model with ID {modelId} not found.");
                return NotFound($"Model with ID {modelId} not found.");
            }
            await SendProgress("Model found in database.");

            // 2. Extract GLB data from the model
            byte[] glbBytes = model.GLBData;
            if (glbBytes == null || glbBytes.Length == 0)
            {
                await SendProgress($"Error: No GLB data found in database for model ID {modelId}. Cannot proceed.");
                return BadRequest($"No GLB data found in database for model ID {modelId}.");
            }
            await SendProgress($"GLB data retrieved from database ({glbBytes.Length} bytes).");

            // Use parameters from the request or fallback to model's stored values
            double zFactor = req.ZFactor ?? model.zFactor;
            double meshReduceFactor = req.MeshReduceFactor ?? model.meshReduceFactor;
            bool includeBuildings = req.IncludeBuildings;
            // Use Quality from request if provided and valid, otherwise fallback or use a default
            int quality = req.quality ; // Example fallback logic


            // 3. Fetch OSM data (as GLB) if buildings are included
            byte[] osmGlbBytes = null;
            string osmSavePath = null; // To store the path if saved

            if (includeBuildings)
            {
                await SendProgress("Fetching OSM GLB data (including buildings)...");
                string clientId = "EKdiPDmHhZ8jiB4z72m5Aw"; // Replace with your actual client ID or get from config
                string osmApiUrl = $"https://api.elevationapi.com/api/model/3d/bbox/osm/" +
                                    $"{model.WestLng},{model.EastLng},{model.SouthLat},{model.NorthLat}" +
                                    "?dataset=AW3D30&textured=true&imageryProvider=MapBox-SatelliteStreet&textureQuality=2&format=glTF&zFactor=1&withHighways=true&highwaysColor=%23DDDDDD&" +
                                     "withBuildings=true&\r\nwithBuildingsColors=false&buildingsColor=%23DCAA86&withSkiPistes=false&withTerrain=false&adornments=false&meshReduceFactor=0.5&" +
                                       "clientConnectionId=EKdiPDmHhZ8jiB4z72m5Aw&onlyEstimateSize=false";

                string osmJson = null;
                string fullOsmGlbUrl = null;
                try
                {
                    await SendProgress($"Requesting OSM metadata from API..."); // Don't log full URL with client ID if sensitive
                    HttpResponseMessage osmMetaResponse = await _httpClient.GetAsync(osmApiUrl);
                    if (!osmMetaResponse.IsSuccessStatusCode)
                    {
                        string errorContent = await osmMetaResponse.Content.ReadAsStringAsync();
                        await SendProgress($"Error fetching OSM metadata: {osmMetaResponse.StatusCode}");
                        Console.WriteLine($"OSM Metadata Fetch Error: {osmMetaResponse.StatusCode} - {errorContent}"); // Log details
                        return StatusCode((int)osmMetaResponse.StatusCode, $"Failed to fetch OSM GLB metadata.");
                    }
                    osmJson = await osmMetaResponse.Content.ReadAsStringAsync();
                    await SendProgress("OSM metadata received.");

                    using (var osmDoc = JsonDocument.Parse(osmJson)) // Use using for disposable JsonDocument
                    {
                        var osmRoot = osmDoc.RootElement;
                        if (!osmRoot.TryGetProperty("assetInfo", out var osmAssetInfo) || !osmAssetInfo.TryGetProperty("modelFile", out var osmFileElement))
                        {
                            await SendProgress("Error: Could not parse OSM download URL from metadata.");
                            Console.WriteLine($"OSM Metadata Parse Error: Could not find assetInfo.modelFile in response: {osmJson}");
                            return StatusCode(500, "Could not parse OSM download URL from metadata.");
                        }
                        string osmFilePath = osmFileElement.GetString();
                        fullOsmGlbUrl = $"https://api.elevationapi.com{osmFilePath}";
                    } // osmDoc is disposed here

                    await SendProgress($"OSM download URL parsed.");
                    await SendProgress("Downloading OSM GLB file...");
                    HttpResponseMessage osmDownloadResponse = await _httpClient.GetAsync(fullOsmGlbUrl);
                    if (!osmDownloadResponse.IsSuccessStatusCode)
                    {
                        string errorContent = await osmDownloadResponse.Content.ReadAsStringAsync();
                        await SendProgress($"Error downloading OSM GLB file: {osmDownloadResponse.StatusCode}");
                        Console.WriteLine($"OSM Download Error: {osmDownloadResponse.StatusCode} - {errorContent}");
                        return StatusCode((int)osmDownloadResponse.StatusCode, $"Failed to download OSM GLB file.");
                    }
                    osmGlbBytes = await osmDownloadResponse.Content.ReadAsByteArrayAsync();
                    await SendProgress($"OSM GLB file downloaded ({osmGlbBytes.Length} bytes).");
                }
                catch (JsonException jsonEx)
                {
                    await SendProgress($"Error parsing OSM JSON metadata: {jsonEx.Message}");
                    Console.WriteLine($"OSM JSON Parse Exception: {jsonEx}");
                    return StatusCode(500, $"Failed to parse OSM API response: {jsonEx.Message}");
                }
                catch (HttpRequestException httpEx)
                {
                    await SendProgress($"Network error fetching OSM data: {httpEx.Message}");
                    Console.WriteLine($"OSM HTTP Request Exception: {httpEx}");
                    return StatusCode(503, $"Network error fetching OSM data: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    await SendProgress($"An unexpected error occurred during OSM fetch: {ex.Message}");
                    Console.WriteLine($"Unhandled exception during OSM fetch for model {modelId}: {ex}");
                    return StatusCode(500, $"An unexpected error occurred during OSM fetch: {ex.Message}");
                }
            }
            else
            {
                await SendProgress("Skipping OSM data fetch as IncludeBuildings is false.");
            }


            // ------------------------------------------------------------------
            // STEP 4: Define Save Location and Save Files
            // ------------------------------------------------------------------
            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "models");
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string modelSpecificFolder = modelId.ToString();
            string finalDirectoryPath = Path.Combine(basePath, dateFolder, modelSpecificFolder);

            try
            {
                Directory.CreateDirectory(finalDirectoryPath); // Ensure the full directory path exists
            }
            catch (Exception dirEx)
            {
                await SendProgress($"Error creating directory '{finalDirectoryPath}': {dirEx.Message}");
                Console.WriteLine($"Directory Creation Error: {dirEx}");
                return StatusCode(500, $"Error creating directory: {dirEx.Message}");
            }


            // Save the GLB (terrain) from the database
            string terrainGlbFileName = "terrain.glb";
            string terrainGlbSavePath = Path.Combine(finalDirectoryPath, terrainGlbFileName);
            try
            {
                await SendProgress($"Saving database GLB (terrain) to: {terrainGlbSavePath}");
                await System.IO.File.WriteAllBytesAsync(terrainGlbSavePath, glbBytes);
                await SendProgress("Database GLB (terrain) saved successfully.");
            }
            catch (Exception ex)
            {
                await SendProgress($"Error saving database GLB (terrain): {ex.Message}");
                Console.WriteLine($"Terrain GLB Save Error: {ex}");
                return StatusCode(500, $"Error saving database GLB (terrain) file: {ex.Message}");
            }

            // Save the OSM GLB (buildings) if it was fetched
            if (osmGlbBytes != null)
            {
                string buildingsGlbFileName = "buildings.glb";
                osmSavePath = Path.Combine(finalDirectoryPath, buildingsGlbFileName);
                try
                {
                    await SendProgress($"Saving OSM GLB (buildings) to: {osmSavePath}");
                    await System.IO.File.WriteAllBytesAsync(osmSavePath, osmGlbBytes);
                    await SendProgress("OSM GLB (buildings) saved successfully.");
                }
                catch (Exception ex)
                {
                    await SendProgress($"Error saving OSM GLB (buildings): {ex.Message}");
                    Console.WriteLine($"Buildings GLB Save Error: {ex}");
                    return StatusCode(500, $"Error saving OSM GLB (buildings) file: {ex.Message}");
                }
            }

            // ------------------------------------------------------------------
            // STEP 4.5: Create Blank STL File
            // ------------------------------------------------------------------
            string blankStlFileName = "outputfile.stl";
            string blankStlSavePath = Path.Combine(finalDirectoryPath, blankStlFileName);
            try
            {
                await SendProgress($"Creating blank STL file at: {blankStlSavePath}");
                // Create an empty file. Using WriteAllTextAsync with empty string is simple.
                await System.IO.File.WriteAllTextAsync(blankStlSavePath, string.Empty);
                // Alternative: File.Create(blankStlSavePath).Dispose();
                await SendProgress("Blank STL file created successfully.");
            }
            catch (Exception ex)
            {
                await SendProgress($"Error creating blank STL file: {ex.Message}");
                Console.WriteLine($"Blank STL Create Error: {ex}");
                // Decide if this is critical enough to stop the process. Maybe just warn?
                // For now, let's return an error as the user requested its creation.
                return StatusCode(500, $"Error creating blank STL file: {ex.Message}");
            }
            // ------------------------------------------------------------------


            // 5. Final Response (No STL processing or DB update in this version)
            await SendProgress("Local file saving process complete.");

            if (includeBuildings)
            {
                ModelProcessor.ProcessModels(terrainGlbSavePath, osmSavePath, blankStlSavePath);

            }
            else
            {
                ModelProcessor.ProcessModels(terrainGlbSavePath, blankStlSavePath);
            }
            await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveProgress", "Local STL file generated.");

            // 6. Check if the user is authenticated by looking for the JWT token
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

            if (!System.IO.File.Exists(blankStlSavePath))
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveProgress", "Error: Final STL file not found on disk.");
                return StatusCode(500, "STL file was not generated.");
            }
            byte[] finalStlBytes = await System.IO.File.ReadAllBytesAsync(blankStlSavePath);

            // ------------------------------------------------------------------
            // STEP 6: Save the final STL data to the model record.
            // ------------------------------------------------------------------
            model.STLData = finalStlBytes;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveProgress", "Model record updated with STL file.");

            return Ok(new
            {
                Message = includeBuildings
                            ? "STL conversion complete with buildings merged."
                            : "STL conversion complete (terrain only).",
                STLSize = finalStlBytes.Length,
                TerrainGlbPath = terrainGlbSavePath,
                BuildingsGlbPath = osmSavePath,
                FinalStlPath = blankStlSavePath
            });
        }



     }
}
