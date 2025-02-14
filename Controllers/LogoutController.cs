using Microsoft.AspNetCore.Mvc;

[Route("auth")]
[ApiController]
public class LogoutController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogoutController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _httpContextAccessor.HttpContext.Session.Clear(); // Clear session
        return Ok(new { message = "Logged out successfully", redirectTo = "/auth/login" });
    }
}
