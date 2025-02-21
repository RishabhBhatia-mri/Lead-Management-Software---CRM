using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeadManagment.Models;
using System.Security.Claims;

[Route("kanban")]
[ApiController]
public class KanbanController : ControllerBase {
    private readonly LeadsManagementContext _context;

    public KanbanController(LeadsManagementContext context) {
        _context = context;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminKanban() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(roleClaim)) {
            return Unauthorized(new { message = "Invalid token. Admin ID or role missing." });
        }

        var leads = _context.Leads.ToList();
        return Ok(GroupLeadsByStatus(leads));
    }

    [HttpGet("manager")]
    [Authorize(Roles = "Manager")]
    public IActionResult GetManagerKanban() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(roleClaim)) {
            return Unauthorized(new { message = "Invalid token. Manager ID or role missing." });
        }

        int managerId = int.Parse(userIdClaim);
        var leads = _context.Leads
            .Where(l => l.ManagerAssigned == managerId)
            .ToList();

        return Ok(GroupLeadsByStatus(leads));
    }

    [HttpGet("salesRep")]
    [Authorize]
    public IActionResult GetSalesRepKanban() {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(roleClaim) || (roleClaim != "Sales Representative" && roleClaim != "SalesRep")) {
            return Unauthorized(new { message = "Invalid role. Only Sales Representatives can access this." });
        }

        int salesRepId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var leads = _context.Leads
            .Where(l => l.SalesRepAssigned == salesRepId)
            .ToList();

        return Ok(GroupLeadsByStatus(leads));
    }


    private Dictionary<string, List<dynamic>> GroupLeadsByStatus(List<Lead> leads) {
        return leads
            .GroupBy(l => l.Status)
            .ToDictionary(g => g.Key, g => g.Select(l => new {
                l.Lid,
                l.Name,
                l.Email,
                l.Phone,
                l.Source,
                l.Status,
                l.ManagerAssigned,
                l.SalesRepAssigned,
                l.CreatedBy,
                l.CreatedAt
            }).ToList<dynamic>());
    }
}
