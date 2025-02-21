using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using LeadManagment.Models;

namespace LeadManagment.Controllers {
    [Route("management/assignment")]
    [ApiController]
    [Authorize(Roles = "Manager")] // Only managers can access this controller
    public class ManagementAssignmentController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ManagementAssignmentController(LeadsManagementContext context) {
            _context = context;
        }

        // GET API: Fetch all leads assigned to the manager
        [HttpGet]
        public IActionResult GetLeadAssignments() {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            var allLeads = _context.Leads
                .Where(l => l.ManagerAssigned == managerId)
                .Select(l => new {
                    l.Lid,
                    l.Name,
                    l.Email,
                    l.Phone,
                    l.Status,
                    SalesRep_Assigned = l.SalesRepAssigned,
                    SalesRep_Name = _context.Users
                        .Where(u => u.Uid == l.SalesRepAssigned)
                        .Select(u => u.Name)
                        .FirstOrDefault()
                })
                .OrderBy(l => l.SalesRep_Assigned == null ? 0 : 1) // Unassigned leads first
                .ThenBy(l => l.Lid)
                .ToList();

            return Ok(new {
                totalLeads = allLeads.Count,
                leadList = allLeads
            });
        }

        // POST API: Assign a lead to a sales representative
        [HttpPost("{leadId}/assign")]
        public IActionResult AssignLead(int leadId, [FromBody] AssignLeadRequest request) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Check if the lead exists and belongs to the manager
            var lead = _context.Leads.FirstOrDefault(l => l.Lid == leadId && l.ManagerAssigned == managerId);
            if (lead == null) {
                return NotFound(new { message = "Lead not found or not under your management." });
            }

            // Validate that the provided sales rep ID belongs to the manager's team
            bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == request.SalesRepId && u.ReportsTo == managerId);
            if (!isSalesRepUnderManager) {
                return BadRequest(new { message = "Invalid Sales Rep ID. The user is not under your management." });
            }

            // Assign the lead
            lead.SalesRepAssigned = request.SalesRepId;
            _context.SaveChanges();

            return Ok(new { message = "Lead assigned successfully." });
        }

        public class AssignLeadRequest {
            public int SalesRepId { get; set; }
        }
    }
}
