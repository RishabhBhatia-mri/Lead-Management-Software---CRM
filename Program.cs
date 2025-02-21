using LeadManagment.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml; // Add the EPPlus namespace
using System.Text;
using LeadManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<LeadsManagementContext>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminService>();


// Set the EPPlus License Context
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial for commercial use

builder.Services.AddControllers();

// Configure database context
builder.Services.AddDbContext<LeadsManagementContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("lmdb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("lmdb"))));

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// JWT token config
var jwtSettings = builder.Configuration.GetSection("JwtConfig");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // Ensure token expiration is enforced
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero // Ensures token expires at exact time, no grace period
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseCors("AllowAll"); // Move this above app.UseRouting()

app.UseRouting();
app.UseSession();
app.UseAuthentication();
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
