using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLeadById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var lead = await _context.Leads
            .Include(l => l.ManagerAssignedNavigation)
            .Include(l => l.SalesRepAssignedNavigation)
            .FirstOrDefaultAsync(l => l.Lid == id);

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

        return Ok(new
        {
            LeadId = lead.Lid,
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Status = lead.Status,
            ManagerAssigned = lead.ManagerAssignedNavigation?.Name,
            SalesRepAssigned = lead.SalesRepAssignedNavigation?.Name,
            CreatedBy = lead.CreatedBy,
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt,
            AssignedAt = lead.AssignedAt
        });
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

        // Lead's ManagerAssigned must match SalesRep's ReportsTo
        if (userRole == "Admin")
        {
            if (lead.ManagerAssigned == null || lead.ManagerAssigned != salesRep.ReportsTo)
            {
                return BadRequest(new { message = "The assigned Sales Rep must report to the lead's assigned Manager." });
            }
        }
        // Lead must be assigned to them & Sales Rep must report to them
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

    [HttpPatch("status/{leadId}")]
    public async Task<IActionResult> UpdateLeadStatus(int leadId, [FromBody] JsonElement requestBody)
    {
        // Get user ID and role from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userRoleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || userRoleClaim == null)
        {
            return Unauthorized("User ID or Role not found in token.");
        }

        int userId = int.Parse(userIdClaim.Value);
        string userRole = userRoleClaim.Value;

        // Extract status from request body
        if (!requestBody.TryGetProperty("status", out JsonElement statusElement) || statusElement.ValueKind != JsonValueKind.String)
        {
            return BadRequest("Invalid request. 'status' field is required and must be a string.");
        }

        string newStatus = statusElement.GetString()?.Trim();

        // Validate the new status
        if (string.IsNullOrEmpty(newStatus) || !new[] { "New", "Contacted", "Follow-up", "Converted", "Lost" }.Contains(newStatus))
        {
            return BadRequest("Invalid status value.");
        }

        // Find the lead
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return NotFound("Lead not found.");
        }

        string oldStatus = lead.Status ?? "New"; // Default to "New" if null

        // Check if the status is actually changing
        if (oldStatus == newStatus)
        {
            return BadRequest("Lead status is already set to the requested status.");
        }

        // **Authorization Check**
        if (userRole == "Admin")
        {
            // Admin can update any lead status
        }
        else if (userRole == "Manager")
        {
            // Manager can update leads assigned to them or their Sales Representatives
            bool isManagerAssigned = lead.ManagerAssigned == userId;
            bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == userId);

            if (!isManagerAssigned && !isSalesRepUnderManager)
            {
                return Forbid("You are not authorized to update this lead.");
            }
        }
        else if (userRole == "Sales Representative")
        {
            // Sales Representative can only update their assigned leads
            if (lead.SalesRepAssigned != userId)
            {
                return Forbid("You are not authorized to update this lead.");
            }
        }
        else
        {
            return Forbid("Invalid role.");
        }

        // **Update lead status**
        lead.Status = newStatus;
        lead.UpdatedAt = DateTime.UtcNow;

        // Insert into LeadStatusHistory
        var statusHistory = new LeadStatusHistory
        {
            Lid = leadId,
            Uid = userId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            TimeOfChange = DateTime.UtcNow
        };
        _context.LeadStatusHistories.Add(statusHistory);

        // Insert into LeadUpdateLog
        var updateLog = new LeadUpdateLog
        {
            Lid = leadId,
            Uid = userId,
            FieldUpdated = "Status",
            OldValue = oldStatus,
            NewValue = newStatus,
            UpdatedAt = DateTime.UtcNow
        };
        _context.LeadUpdateLogs.Add(updateLog);

        // Save changes
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Lead status updated successfully.",
            LeadId = leadId,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }

    [HttpPost("notes/{leadId}")]
    public async Task<IActionResult> AddLeadNote(int leadId, [FromBody] JsonElement requestBody)
    {
        // Get user ID and role from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userRoleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || userRoleClaim == null)
        {
            return Unauthorized("User ID or Role not found in token.");
        }

        int userId = int.Parse(userIdClaim.Value);
        string userRole = userRoleClaim.Value;

        // Extract notes from request body
        if (!requestBody.TryGetProperty("notes", out JsonElement notesElement) || notesElement.ValueKind != JsonValueKind.String)
        {
            return BadRequest("Invalid request. 'notes' field is required and must be a string.");
        }

        string notes = notesElement.GetString()?.Trim();

        // Validate notes
        if (string.IsNullOrEmpty(notes))
        {
            return BadRequest("Notes cannot be empty.");
        }

        // Find the lead
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return NotFound("Lead not found.");
        }

        // **Authorization Check**
        if (userRole == "Admin")
        {
            return Forbid("Admins are not allowed to add notes.");
        }
        else if (userRole == "Manager")
        {
            // Manager can add notes only if the lead is assigned to them or their Sales Representative
            bool isManagerAssigned = lead.ManagerAssigned == userId;
            bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == userId);

            if (!isManagerAssigned && !isSalesRepUnderManager)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }
        else if (userRole == "Sales Representative")
        {
            // Sales Representative can add notes only to their assigned leads
            if (lead.SalesRepAssigned != userId)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }
        else
        {
            return Forbid("Invalid role.");
        }

        // **Save Note to LeadActivityLog**
        var leadActivityLog = new LeadActivityLog
        {
            Lid = leadId,
            Uid = userId,
            ActivityDate = DateTime.UtcNow,
            Notes = notes,
            Responded = false // Assuming default false
        };

        _context.LeadActivityLogs.Add(leadActivityLog);

        // **Save Follow-up Entry in LeadFollowUps**
        var leadFollowUp = new LeadFollowUp
        {
            Lid = leadId,
            Uid = userId,
            FollowUpDate = DateTime.UtcNow.AddDays(2), // Assuming next follow-up in 2 days
            Status = "Pending",
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadFollowUps.Add(leadFollowUp);

        // Commit changes to database
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Note added successfully and follow-up scheduled.",
            LeadId = leadId,
            AddedBy = userId,
            Notes = notes,
            FollowUpDate = leadFollowUp.FollowUpDate
        });
    }

    [HttpPost("notes/activitylog/{leadId}")]
    public async Task<IActionResult> AddNoteToLeadActivityLog(int leadId, [FromBody] JsonElement requestBody)
    {
        // Get user ID and role from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userRoleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || userRoleClaim == null)
        {
            return Unauthorized("User ID or Role not found in token.");
        }

        int userId = int.Parse(userIdClaim.Value);
        string userRole = userRoleClaim.Value;

        // Extract notes from request body
        if (!requestBody.TryGetProperty("notes", out JsonElement notesElement) || notesElement.ValueKind != JsonValueKind.String)
        {
            return BadRequest("Invalid request. 'notes' field is required and must be a string.");
        }

        string notes = notesElement.GetString()?.Trim();

        // Validate notes
        if (string.IsNullOrEmpty(notes))
        {
            return BadRequest("Notes cannot be empty.");
        }

        // Find the lead
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return NotFound("Lead not found.");
        }

        // **Authorization Check**
        if (userRole == "Admin")
        {
            return Forbid("Admins are not allowed to add notes.");
        }
        else if (userRole == "Manager")
        {
            bool isManagerAssigned = lead.ManagerAssigned == userId;
            bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == userId);

            if (!isManagerAssigned && !isSalesRepUnderManager)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }
        else if (userRole == "Sales Representative")
        {
            if (lead.SalesRepAssigned != userId)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }

        // **Add note to LeadActivityLog**
        var leadActivityLog = new LeadActivityLog
        {
            Lid = leadId,
            Uid = userId,
            ActivityDate = DateTime.UtcNow,
            Notes = notes,
            Responded = false
        };

        _context.LeadActivityLogs.Add(leadActivityLog);

        // Commit changes to database
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Note added to activity log successfully.",
            LeadId = leadId,
            AddedBy = userId,
            Notes = notes
        });
    }

    [HttpPost("notes/followup/{leadId}")]
    public async Task<IActionResult> AddNoteAndStatusToLeadFollowUp(int leadId, [FromBody] JsonElement requestBody)
    {
        // Get user ID and role from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userRoleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || userRoleClaim == null)
        {
            return Unauthorized("User ID or Role not found in token.");
        }

        int userId = int.Parse(userIdClaim.Value);
        string userRole = userRoleClaim.Value;

        // Extract notes and status from the request body
        if (!requestBody.TryGetProperty("notes", out JsonElement notesElement) || notesElement.ValueKind != JsonValueKind.String ||
            !requestBody.TryGetProperty("status", out JsonElement statusElement) || statusElement.ValueKind != JsonValueKind.String)
        {
            return BadRequest("Invalid request. 'notes' and 'status' are required and must be valid.");
        }

        string notes = notesElement.GetString()?.Trim();
        string status = statusElement.GetString()?.Trim();
        DateTime followUpDate = DateTime.UtcNow;  // Default follow-up date is set to the current date and time.

        if (string.IsNullOrEmpty(notes))
        {
            return BadRequest("Notes cannot be empty.");
        }

        // Validate the status value is one of the allowed strings
        var validStatuses = new[] { "Pending", "Completed", "Missed" };
        if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status))
        {
            return BadRequest("Invalid status. Please provide a valid status ('Pending', 'Completed', or 'Missed').");
        }

        // Check if follow_up_date was provided in the request
        if (requestBody.TryGetProperty("follow_up_date", out JsonElement followUpDateElement) && followUpDateElement.ValueKind == JsonValueKind.String)
        {
            if (!DateTime.TryParse(followUpDateElement.GetString(), out followUpDate))
            {
                return BadRequest("Invalid date format for follow-up.");
            }
        }

        // Find the lead
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return NotFound("Lead not found.");
        }

        // **Authorization Check**
        if (userRole == "Admin")
        {
            return Forbid("Admins are not allowed to add notes.");
        }
        else if (userRole == "Manager")
        {
            bool isManagerAssigned = lead.ManagerAssigned == userId;
            bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == lead.SalesRepAssigned && u.ReportsTo == userId);

            if (!isManagerAssigned && !isSalesRepUnderManager)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }
        else if (userRole == "Sales Representative")
        {
            if (lead.SalesRepAssigned != userId)
            {
                return Forbid("You are not authorized to add notes to this lead.");
            }
        }

        // **Save Follow-up Entry in LeadFollowUps**
        var leadFollowUp = new LeadFollowUp
        {
            Lid = leadId,
            Uid = userId,
            FollowUpDate = followUpDate,  // Default to current date if not provided
            Status = status,  // Status as string (e.g., 'Pending', 'Completed', 'Missed')
            Notes = notes,  // Notes for the follow-up
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadFollowUps.Add(leadFollowUp);

        // Commit changes to database
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Follow-up created successfully with notes and status.",
            LeadId = leadId,
            AddedBy = userId,
            Notes = notes,
            Status = status,
            FollowUpDate = followUpDate
        });
    }

    //[HttpGet("activity-history/{leadId}")]
    //public async Task<IActionResult> GetLeadActivityLog(int leadId)
    //{
    //    Extract the UserId from the JWT
    //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    //    if (userId == null)
    //    {
    //        return Unauthorized("User ID not found in JWT.");
    //    }

    //    var userIdInt = int.Parse(userId); // Assuming UserId is an integer

    //    Get the user role
    //    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

    //    Check if the lead exists
    //    var lead = await _context.Leads
    //        .Where(l => l.Lid == leadId)
    //        .FirstOrDefaultAsync();

    //    if (lead == null)
    //    {
    //        return NotFound("Lead not found.");
    //    }

    //    Check permissions based on user role
    //    if (userRole == "Admin")
    //    {
    //        Admin can access logs for any lead

    //       var activityLogs = await _context.LeadActivityLogs
    //           .Where(lal => lal.Lid == leadId)
    //           .Include(lal => lal.UidNavigation)  // Includes user information
    //           .ToListAsync();

    //        return Ok(activityLogs);
    //    }
    //    else if (userRole == "Manager")
    //    {
    //        Manager can access logs for leads assigned to their Sales Rep or themselves

    //       var teamMemberIds = await _context.Users
    //                                         .Where(u => u.ReportsTo == userIdInt)
    //                                         .Select(u => u.Uid)
    //                                         .ToListAsync();

    //        if (lead.ManagerAssigned != userIdInt && !teamMemberIds.Contains(lead.SalesRepAssigned ?? 0))
    //            {
    //                return Unauthorized("You do not have access to this lead's activity logs.");
    //            }

    //        var activityLogsForManager = await _context.LeadActivityLogs
    //            .Where(lal => lal.Lid == leadId)
    //            .Include(lal => lal.UidNavigation)  // Includes user information
    //            .ToListAsync();

    //        return Ok(activityLogsForManager);
    //    }
    //    else if (userRole == "Sales Representative")
    //    {
    //        Sales Rep can only access logs for leads assigned to themselves
    //        if (lead.SalesRepAssigned != userIdInt)
    //            {
    //                return Unauthorized("You do not have access to this lead's activity logs.");
    //            }

    //        var activityLogsForSalesRep = await _context.LeadActivityLogs
    //            .Where(lal => lal.Lid == leadId)
    //            .Include(lal => lal.UidNavigation)  // Includes user information
    //            .ToListAsync();

    //        return Ok(activityLogsForSalesRep);
    //    }
    //    else
    //    {
    //        return Unauthorized("Role not authorized to access activity logs.");
    //    }
    //}

    [HttpGet("activity-history")]
    public async Task<IActionResult> GetActivityLogs()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<LeadActivityLog> query = _context.LeadActivityLogs
                .Include(log => log.LidNavigation)
                .Include(log => log.UidNavigation);

            if (userRole == "Admin")
            {
                // Admin can see all activity logs
            }
            else if (userRole == "Manager")
            {
                query = query.Where(log => log.LidNavigation.ManagerAssigned == userId);
            }
            else if (userRole == "Sales Representative")
            {
                query = query.Where(log => log.LidNavigation.SalesRepAssigned == userId);
            }
            else
            {
                return Forbid();
            }

            var activityLogs = await query
                .Select(log => new
                {
                    log.Aid,
                    LeadId = log.Lid,
                    LeadName = log.LidNavigation.Name,
                    ActivityBy = log.UidNavigation.Name,
                    log.ActivityDate,
                    log.Notes,
                    log.Responded
                })
                .ToListAsync();

            return Ok(activityLogs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching activity logs.", error = ex.Message });
        }
    }

    //[HttpGet("activity-history/{leadId}")]
    //public async Task<IActionResult> GetActivityLogsByLead(int leadId)
    //{
    //    try
    //    {
    //        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    //        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

    //        // Fetch the lead to verify role-based access
    //        var lead = await _context.Leads.FindAsync(leadId);
    //        if (lead == null)
    //        {
    //            return NotFound(new { message = "Lead not found." });
    //        }

    //        // Check access permissions based on user role
    //        if (userRole == "Admin")
    //        {
    //            // Admin has access to all leads
    //        }
    //        else if (userRole == "Manager")
    //        {
    //            if (lead.ManagerAssigned != userId)
    //            {
    //                return Forbid();
    //            }
    //        }
    //        else if (userRole == "Sales Representative")
    //        {
    //            if (lead.SalesRepAssigned != userId)
    //            {
    //                return Forbid();
    //            }
    //        }
    //        else
    //        {
    //            return Forbid();
    //        }

    //        // Fetch activity logs for the specified lead
    //        var activityLogs = await _context.LeadActivityLogs
    //            .Where(log => log.Lid == leadId)
    //            .Include(log => log.UidNavigation) // Fetch activity performer
    //            .Select(log => new
    //            {
    //                log.Aid,
    //                LeadId = log.Lid,
    //                ActivityBy = log.UidNavigation.Name,
    //                log.ActivityDate,
    //                log.Notes,
    //                log.Responded
    //            })
    //            .ToListAsync();

    //        return Ok(activityLogs);
    //    }
    //    catch (Exception ex)
    //    {
    //        return StatusCode(500, new { message = "An error occurred while fetching activity logs.", error = ex.Message });
    //    }
    //}

    [HttpGet("activity-history/{leadId}")]
    public async Task<IActionResult> GetActivityLogsByLead(int leadId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Fetch the lead to verify role-based access
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found." });
            }

            // Check access permissions based on user role
            if (userRole == "Admin")
            {
                // Admin has access to all leads
            }
            else if (userRole == "Manager")
            {
                if (lead.ManagerAssigned != userId)
                {
                    return Forbid("You do not have access to this lead's activity logs.");
                }
            }
            else if (userRole == "Sales Representative")
            {
                if (lead.SalesRepAssigned != userId)
                {
                    return Forbid("You do not have access to this lead's activity logs.");
                }
            }
            else
            {
                return Forbid("Unauthorized role access.");
            }

            // Fetch activity logs for the specified lead
            var activityLogs = await _context.LeadActivityLogs
                .Where(log => log.Lid == leadId)
                .Include(log => log.UidNavigation) // Fetch activity performer
                .Select(log => new
                {
                    log.Aid,
                    LeadId = log.Lid,
                    ActivityBy = log.UidNavigation.Name,
                    log.ActivityDate,
                    log.Notes,
                    log.Responded
                })
                .ToListAsync();

            // Return message if no activity logs exist for the lead
            if (activityLogs.Count == 0)
            {
                return Ok(new { message = "No activity logs found for this lead." });
            }

            return Ok(activityLogs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching activity logs.", error = ex.Message });
        }
    }
}
