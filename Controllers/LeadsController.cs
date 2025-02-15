using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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

    //[Authorize]
    //[HttpGet("{id}")]
    //public async Task<IActionResult> GetLeadById(int id)
    //{
    //    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    //    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

    //    var lead = await _context.Leads
    //        .Include(l => l.ManagerAssignedNavigation)
    //        .Include(l => l.SalesRepAssignedNavigation)
    //        .FirstOrDefaultAsync(l => l.Lid == id);

    //    if (lead == null)
    //    {
    //        return NotFound(new { message = "Lead not found" });
    //    }

    //    // Role-based access control
    //    if (userRole == "Manager")
    //    {
    //        var managerLeads = await _context.Leads
    //            .Where(l => l.ManagerAssigned == userId ||
    //                        _context.Users.Any(u => u.ReportsTo == userId && u.Uid == l.SalesRepAssigned))
    //            .Select(l => l.Lid)
    //            .ToListAsync();

    //        if (!managerLeads.Contains(id))
    //            return Forbid();
    //    }
    //    else if (userRole == "Sales Representative" && lead.SalesRepAssigned != userId)
    //    {
    //        return Forbid();
    //    }

    //    return Ok(new
    //    {
    //        LeadId = lead.Lid,
    //        Name = lead.Name,
    //        Email = lead.Email,
    //        Phone = lead.Phone,
    //        Source = lead.Source,
    //        Status = lead.Status,
    //        ManagerAssigned = lead.ManagerAssignedNavigation?.Name,
    //        SalesRepAssigned = lead.SalesRepAssignedNavigation?.Name,
    //        CreatedBy = lead.CreatedBy,
    //        CreatedAt = lead.CreatedAt,
    //        UpdatedAt = lead.UpdatedAt,
    //        AssignedAt = lead.AssignedAt
    //    });
    //}

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

    [Authorize]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] JsonElement requestBody)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound(new { message = "Lead not found" });
        }

        // Role-based access control
        if (userRole == "Manager")
        {
            var managerLeads = await _context.Leads
                .Where(l => l.ManagerAssigned == userId ||
                            _context.Users.Any(u => u.ReportsTo == userId && u.Uid == l.SalesRepAssigned))
                .Select(l => l.Lid)
                .ToListAsync();

            if (!managerLeads.Contains(id))
                return Forbid();
        }
        else if (userRole == "Sales Representative" && lead.SalesRepAssigned != userId)
        {
            return Forbid();
        }

        // Allowed fields to update
        var allowedFields = new List<string> { "Name", "Email", "Phone", "Source" };
        var leadType = typeof(Lead);

        // Iterate over the JSON properties
        foreach (var property in requestBody.EnumerateObject())
        {
            var propName = property.Name;
            var propValue = property.Value.ToString();

            // Check if the property is allowed
            if (!allowedFields.Contains(propName))
                continue;

            var propInfo = leadType.GetProperty(propName);
            if (propInfo != null)
            {
                var oldValue = propInfo.GetValue(lead)?.ToString();
                if (oldValue != propValue)
                {
                    // Update lead property
                    propInfo.SetValue(lead, Convert.ChangeType(propValue, propInfo.PropertyType));

                    // Log the update
                    var leadUpdateLog = new LeadUpdateLog
                    {
                        Lid = id,
                        Uid = userId,
                        FieldUpdated = propName,
                        OldValue = oldValue,
                        NewValue = propValue,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.LeadUpdateLogs.Add(leadUpdateLog);
                }
            }
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Lead updated successfully" });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLead(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound(new { message = "Lead not found" });
        }

        // Role-based access control
        if (userRole == "Manager")
        {
            var managerLeads = await _context.Leads
                .Where(l => l.ManagerAssigned == userId ||
                            _context.Users.Any(u => u.ReportsTo == userId && u.Uid == l.SalesRepAssigned))
                .Select(l => l.Lid)
                .ToListAsync();

            if (!managerLeads.Contains(id))
                return Forbid();
        }
        else if (userRole == "Sales Representative" && lead.SalesRepAssigned != userId)
        {
            return Forbid();
        }

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Lead deleted successfully" });
    }

    [Authorize]
    [HttpPatch("assign/{leadId}")]
    public async Task<IActionResult> AssignLeadToSalesRep(int leadId, [FromBody] JsonElement requestBody)
    {
        if (!requestBody.TryGetProperty("SalesRepAssigned", out JsonElement salesRepElement) || !salesRepElement.TryGetInt32(out int newSalesRepId))
        {
            return BadRequest(new { message = "Sales Representative ID is required and must be an integer." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // Fetch the lead
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return NotFound(new { message = "Lead not found" });
        }

        // Fetch the new Sales Rep
        var salesRep = await _context.Users
            .FirstOrDefaultAsync(u => u.Uid == newSalesRepId && u.Role == "Sales Representative");

        if (salesRep == null)
        {
            return BadRequest(new { message = "Invalid Sales Representative ID" });
        }

        // Prevent Sales Representatives from assigning leads
        if (userRole == "Sales Representative")
        {
            return Forbid();
        }

        // ✅ Admin Condition: Lead's ManagerAssigned must match SalesRep's ReportsTo
        if (userRole == "Admin")
        {
            if (lead.ManagerAssigned == null || lead.ManagerAssigned != salesRep.ReportsTo)
            {
                return BadRequest(new { message = "The assigned Sales Rep must report to the lead's assigned Manager." });
            }
        }
        // ✅ Manager Condition: Lead must be assigned to them & Sales Rep must report to them
        else if (userRole == "Manager")
        {
            if (lead.ManagerAssigned != userId || salesRep.ReportsTo != userId)
            {
                return Forbid();
            }
        }

        // Assign or reassign Sales Rep
        lead.SalesRepAssigned = newSalesRepId;
        lead.AssignedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Lead assigned successfully",
            leadId,
            newSalesRepId
        });
    }

}
