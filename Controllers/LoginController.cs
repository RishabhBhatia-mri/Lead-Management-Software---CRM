using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LeadManagment.Models;

[Route("auth")]
[ApiController]
public class LoginController : ControllerBase {
    private readonly LeadsManagementContext _context;

    public LoginController(LeadsManagementContext context) {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password)) {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _context.Users
            .Where(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (user == null || user.Password != request.Password) // Simple password check
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new { message = "Login successful", redirectTo = "/dashboard" });
    }
}

public class LoginRequest {
    public string Email { get; set; }
    public string Password { get; set; }
}
