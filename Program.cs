using LeadManagment.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure database context
builder.Services.AddDbContext<LeadsManagementContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("lmdb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("lmdb"))));

// JWT token config
var jwtSettings = builder.Configuration.GetSection("JwtConfig");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware order matters
app.UseRouting();

app.UseAuthentication();  // Must be before Authorization
app.UseAuthorization();

app.MapControllers();

// Set default route to /auth/login
app.UseEndpoints(endpoints => {
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "auth/login",
        defaults: new { controller = "Auth", action = "Login" }
    );
});

app.Run();