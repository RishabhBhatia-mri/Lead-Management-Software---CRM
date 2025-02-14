using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LeadManagment.Models;
using Microsoft.Extensions.Configuration;
using System;

[Route("auth")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly LeadsManagementContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginController(LeadsManagementContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _context.Users
            .Where(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (user == null || user.Password != request.Password)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Generate JWT token with Role
        var token = GenerateJwtToken(user);

        // Store token and user details in session
        _httpContextAccessor.HttpContext.Session.SetString("AuthToken", token);
        _httpContextAccessor.HttpContext.Session.SetString("UserRole", user.Role); // Store role in session

        return Ok(new { message = "Login successful", token, redirectTo = "/dashboard" });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtConfig");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var claims = new[] {
        new Claim(JwtRegisteredClaimNames.Sub, user.Uid.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role), // Include Role in Token
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Exp,
            new DateTimeOffset(DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["TokenValidityMins"]))).ToUnixTimeSeconds().ToString()) // Expiry claim
    };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["TokenValidityMins"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
