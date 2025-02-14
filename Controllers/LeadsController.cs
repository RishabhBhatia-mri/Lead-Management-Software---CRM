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

    [HttpPost("create")]
    public async Task<IActionResult> CreateLead([FromBody] Lead lead)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userRole = User.FindFirst(ClaimTypes.Role).Value;

        // Create a new lead object
        Lead newLead = new Lead
        {
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Status = lead.Status,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            AssignedAt = DateTime.UtcNow
        };

        // Handle Sales Representative role
        if (userRole == "Sales Representative")
        {
            if (lead.ManagerAssigned != null || lead.SalesRepAssigned != null)
            {
                return BadRequest(new { message = "Sales Representatives cannot assign Manager or Sales Representative to the lead." });
            }

            // Automatically assign ManagerAssigned to the Sales Rep's Manager
            var manager = await _context.Users
                .Where(u => u.Uid == userId)
                .Select(u => u.ReportsTo)
                .FirstOrDefaultAsync();

            if (manager == null)
            {
                return BadRequest(new { message = "Sales Representative does not have a Manager assigned." });
            }

            newLead.ManagerAssigned = manager;
            newLead.SalesRepAssigned = userId; // Automatically assign Sales Rep to themselves
        }
        // Handle Manager role
        else if (userRole == "Manager")
        {
            if (lead.SalesRepAssigned != null)
            {
                var validSalesRep = await _context.Users
                    .AnyAsync(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == userId);

                if (!validSalesRep)
                {
                    return BadRequest(new { message = "You can only assign Sales Representatives under your management." });
                }
            }

            newLead.ManagerAssigned = userId;
            newLead.SalesRepAssigned = lead.SalesRepAssigned;
        }
        // Handle Admin role
        else if (userRole == "Admin")
        {
            if (lead.ManagerAssigned != null)
            {
                var managerExists = await _context.Users
                    .AnyAsync(u => u.Uid == lead.ManagerAssigned && u.Role == "Manager");

                if (!managerExists)
                {
                    return BadRequest(new { message = "The specified Manager does not exist." });
                }
            }

            if (lead.SalesRepAssigned != null)
            {
                var validSalesRep = await _context.Users
                    .AnyAsync(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == lead.ManagerAssigned);

                if (!validSalesRep)
                {
                    return BadRequest(new { message = "You can only assign Sales Representatives under the specified Manager." });
                }
            }

            newLead.ManagerAssigned = lead.ManagerAssigned ?? userId; // Use Admin's ID if ManagerAssigned is null
            newLead.SalesRepAssigned = lead.SalesRepAssigned;
        }
        else
        {
            return Unauthorized(new { message = "Invalid role. Access denied." });
        }

        // Add the new lead to the Leads table
        _context.Leads.Add(newLead);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Lead created successfully.", lead = newLead });
    }
}
