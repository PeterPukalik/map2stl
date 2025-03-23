using map2stl.DB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//entity framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // for dev
        options.SaveToken = true;
        var secretKey = builder.Configuration["JwtSettings:SecretKey"];
        if(secretKey == null)
        {
            throw new Exception("Secret key is missing");
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),//appsetting.development.json
            RoleClaimType = ClaimTypes.Role 

        };
    });

builder.Services.AddAuthorization();

// Register HttpClient properly
builder.Services.AddHttpClient();

// Configure CORS to allow requests from http://localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowed(origin => true));
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Serve static files from the "models" directory
var modelDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models");
Console.WriteLine($"Serving models from: {modelDirectory}");

// Use CORS
app.UseCors("AllowReactApp");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(modelDirectory),
    RequestPath = "/models",
    ServeUnknownFileTypes = true, // Allow unknown file types
    DefaultContentType = "application/octet-stream" // Set default MIME type

});
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Using connection string: {connection}");

app.UseRouting();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();



