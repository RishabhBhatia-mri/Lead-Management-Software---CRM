using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("leads")]
[ApiController]
[Authorize] 
public class LeadController : ControllerBase
{
    private readonly LeadsManagementContext _context;

    public LeadController(LeadsManagementContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeads()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userRole = User.FindFirst(ClaimTypes.Role).Value;

        var query = _context.Leads.AsQueryable();

        if (userRole == "Sales Representative")
        {
            query = query.Where(l => l.SalesRepAssigned == userId);
        }
        else if (userRole == "Manager")
        {
            var salesRepIds = await _context.Users
                .Where(u => u.ReportsTo == userId)
                .Select(u => u.Uid) 
                .ToListAsync();

            query = query.Where(l => salesRepIds.Contains(l.SalesRepAssigned ?? 0) || l.ManagerAssigned == userId);
        }
        else if (userRole == "Admin")
        {
            // Admin can see all leads
            // No filtering needed for Admin
        }
        else
        {
            return Unauthorized(new { message = "Invalid role. Access denied." });
        }

        var leads = await query.ToListAsync();

        return Ok(new { message = "Leads fetched successfully.", leads });
    }
}
