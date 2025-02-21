using LeadManagment.Controllers;
using LeadManagment.Dashboards;
using LeadManagment.Dashboards.LeadManagment.Dashboards;
using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


[Route("dashboard")]
[ApiController]
[Authorize] // Ensures only authenticated users can access
public class DashboardController : ControllerBase {
    private readonly LeadsManagementContext _context;

    public DashboardController(LeadsManagementContext context) {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetDashboard() {
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

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
            return Unauthorized(new { message = "Invalid token. User ID missing." });
        }
        object dashboardContent = role switch {
            "Admin" => new AdminDashboard(_context).GetContent(),
            "Manager" => new ManagerDashboard(_context).GetContent(userId),
            "Sales Representative" => new SalesDashboard(_context).GetSalesRepLeadsCount(userId).Result, 
            _ => null
        };


        if (dashboardContent == null) {
            return Forbid();
        }

        return Ok(new { message = "Welcome to the dashboard", role, dashboard = dashboardContent });
    }

}
