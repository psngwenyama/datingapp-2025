using System.IdentityModel.Tokens.Jwt;
using System.Text;
using API.Data;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Add services to the dependency injection (DI) container
// ------------------------------------------------------------

// Adds support for controllers (API endpoints)
builder.Services.AddControllers();

// Register the AppDbContext and configure it to use SQLite as the database provider
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    // The connection string is fetched from appsettings.json under "DefaultConnection"
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Enable Cross-Origin Resource Sharing (CORS)
builder.Services.AddCors();

// Register the token service for dependency injection
// This service will be used to generate JWT tokens for user authentication
builder.Services.AddScoped<ITokenService, TokenService>();

// ------------------------------------------------------------
// Configure JWT Authentication
// ------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Retrieve the secret key for signing tokens from configuration (appsettings.json or environment variable)
        var tokenKey = builder.Configuration["TokenKey"]
            ?? throw new Exception("Token key not found - Program.cs");

        // Define how incoming JWT tokens should be validated
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true, // Validate the signing key used to generate the token
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)), // Specify the secret key
            ValidateIssuer = false,  // Skip issuer validation (can be set to true if you have a known issuer)
            ValidateAudience = false // Skip audience validation (can be set to true if you have a known audience)
        };
    });

// ------------------------------------------------------------
// Build the Web Application
// ------------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------------
// Configure the HTTP request pipeline (middleware setup)
// ------------------------------------------------------------

// Allow cross-origin requests from Angular dev server (localhost:4200)
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(x => x
    .AllowAnyHeader()   // Allow any HTTP header (e.g., Authorization, Content-Type)
    .AllowAnyMethod()   // Allow any HTTP method (GET, POST, PUT, DELETE, etc.)
    .WithOrigins("http://localhost:4200", "https://localhost:4200")); // Allow requests from these origins

// Enable authentication middleware to check JWT tokens in incoming requests
app.UseAuthentication();

// Enable authorization middleware to enforce access rules (e.g., [Authorize] attributes)
app.UseAuthorization();

// Map controllers to handle incoming API requests (routes defined in controllers)
app.MapControllers();

// Run the application
app.Run();
