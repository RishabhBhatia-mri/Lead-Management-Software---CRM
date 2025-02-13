using LeadManagment.Dashboards;
using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[Route("dashboard")]
[ApiController]
[Authorize] // Ensures only authenticated users can access
public class DashboardController : ControllerBase {

    [HttpGet]
    public IActionResult GetDashboard() {
        // Check if the token is expired
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (expClaim != null && long.TryParse(expClaim, out long exp)) {
            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            if (DateTime.UtcNow > expiryDate) {
                return Unauthorized(new { message = "Session expired. Please log in again.", redirectTo = "/auth/login" });
            }
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role)) {
            return Unauthorized(new { message = "Invalid role. Access denied." });
        }

        object dashboardContent = role switch {
            "Admin" => new AdminDashboard().GetContent(),
            "Manager" => new ManagerDashboard().GetContent(),
            "Sales Representative" => new SalesDashboard().GetContent(),
            _ => null
        };

        if (dashboardContent == null) {
            return Forbid();
        }

        return Ok(new { message = "Welcome to the dashboard", role, dashboard = dashboardContent });
    }
}
