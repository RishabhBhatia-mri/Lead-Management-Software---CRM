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
using LeadManagement.Helpers;
using System;

[Route("auth")]
[ApiController]
public class LoginController : ControllerBase {
    private readonly LeadsManagementContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginController(LeadsManagementContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor) {
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password)) {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _context.Users
            .Where(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.Password)) {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Generate JWT token with Role
        var token = GenerateJwtToken(user);

        // Store token and user details in session
        _httpContextAccessor.HttpContext.Session.SetString("AuthToken", token);
        _httpContextAccessor.HttpContext.Session.SetString("UserRole", user.Role); // Store role in session

        return Ok(new { message = "Login successful", token, redirectTo = "/dashboard" });
    }

    private string GenerateJwtToken(User user) {
        var jwtSettings = _configuration.GetSection("JwtConfig");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Uid.ToString()), // Correct user ID claim
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Exp,
            new DateTimeOffset(DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["TokenValidityMins"])))
            .ToUnixTimeSeconds().ToString())
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

    [HttpGet("user-role")]
    public IActionResult GetUserRole() {
        var userRole = _httpContextAccessor.HttpContext.Session.GetString("UserRole");
        var userId = _httpContextAccessor.HttpContext.Session.GetString("UserId");

        if (!string.IsNullOrEmpty(userRole) && !string.IsNullOrEmpty(userId)) {
            return Ok(new { role = userRole, userId = userId });
        }

        var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ")) {
            return Unauthorized(new { message = "Unauthorized: Token missing or invalid." });
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        var jwtHandler = new JwtSecurityTokenHandler();

        if (!jwtHandler.CanReadToken(token)) {
            return Unauthorized(new { message = "Invalid token." });
        }

        var jwtToken = jwtHandler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (roleClaim == null || userIdClaim == null) {
            return Unauthorized(new { message = "Required claims not found in token." });
        }

        return Ok(new { role = roleClaim.Value, userId = userIdClaim.Value });
    }

}

public class LoginRequest {
    public string Email { get; set; }
    public string Password { get; set; }
}
