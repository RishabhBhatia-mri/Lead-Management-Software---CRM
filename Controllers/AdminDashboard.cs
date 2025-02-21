using System.Security.Claims;
using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace LeadManagment.Dashboards {
    namespace LeadManagment.Dashboards {
        [Route("dashboard/admin")]
        [ApiController]
        [Authorize(Roles = "Admin")]   // Ensure only admins can access
        public class AdminDashboard : ControllerBase {
            private readonly LeadsManagementContext _context;

            public AdminDashboard(LeadsManagementContext context) {
                _context = context ?? throw new ArgumentNullException(nameof(context));
            }

            // Fetch count of leads for all the managers under the admin
            [HttpGet("count")]
            public IActionResult GetCount() {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminId)) {
                    return Unauthorized(new { message = "Invalid token. Admin ID missing." });
                }

                // Get all the managers under the admin
                var managersUnderAdmin = _context.Users
                    .Where(u => u.ReportsTo == adminId)  // Assuming the 'ReportsTo' field is used for hierarchy
                    .ToList();

                // Get leads for the managers under the admin
                var leadStatusCounts = _context.Leads
                    .Where(l => managersUnderAdmin.Select(m => m.Uid).Contains(l.ManagerAssigned ?? 0))  // Get leads of managers under the admin
                    .GroupBy(l => l.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Count of leads per status for all managers under the admin
                int totalLeads = leadStatusCounts.Values.Sum();
                int newLeads = leadStatusCounts.GetValueOrDefault("New", 0);
                int contactedLeads = leadStatusCounts.GetValueOrDefault("Contacted", 0);
                int followUpLeads = leadStatusCounts.GetValueOrDefault("Follow-up", 0);
                int convertedLeads = leadStatusCounts.GetValueOrDefault("Converted", 0);
                int lostLeads = leadStatusCounts.GetValueOrDefault("Lost", 0);

                return Ok(new {
                    totalLeads,
                    newLeads,
                    contactedLeads,
                    followUpLeads,
                    convertedLeads,
                    lostLeads,
                    leadStatusCounts,
                    totalManagers = managersUnderAdmin.Count  // Added count of managers under the admin
                });
            }


            // Fetch details of all the managers under the admin
            [HttpGet("managers/details")]
            public IActionResult GetContent() {
                // Ensure _context is not null
                if (_context == null) {
                    return StatusCode(500, new { message = "Database context is not initialized." });
                }

                // Get the admin's ID from the JWT token claim
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminId)) {
                    return Unauthorized(new { message = "Invalid token. Admin ID missing." });
                }

                // Get all the managers under the admin
                var managersUnderAdmin = _context.Users
                    .Where(u => u.ReportsTo == adminId)
                    .Select(u => new {
                        u.Uid,
                        u.Name,
                        u.PhoneNo,
                        u.DateOfJoining
                    })
                    .ToList();

                // Check if no managers were found
                if (managersUnderAdmin == null || !managersUnderAdmin.Any()) {
                    return Ok(new { message = "No managers found under this admin.", managers = new List<object>() });
                }

                // Get leads assigned to managers
                var managersWithLeads = managersUnderAdmin.Select(manager => new {
                    manager.Uid,
                    manager.Name,
                    LeadsAssigned = _context.Leads.Count(l => l.ManagerAssigned == manager.Uid),
                    FollowUpLeads = _context.Leads.Count(l => l.ManagerAssigned == manager.Uid && l.Status == "Follow-up"),
                    ConvertedLeads = _context.Leads.Count(l => l.ManagerAssigned == manager.Uid && l.Status == "Converted")
                }).ToList();

                return Ok(new {
                    message = "Managers detailed data fetched successfully",
                    totalManagers = managersUnderAdmin.Count,
                    managers = managersWithLeads
                });
            }



            // Fetch lead assignments for managers under the admin
            [HttpGet("assignment")]
            public IActionResult GetLeadAssignments() {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminId)) {
                    return Unauthorized(new { message = "Invalid token. Admin ID missing." });
                }

                // Get all the managers under the admin
                var managersUnderAdmin = _context.Users
                    .Where(u => u.ReportsTo == adminId)
                    .ToList();

                // Get all leads for the managers under the admin
                var allLeads = _context.Leads
                    .Where(l => managersUnderAdmin.Select(m => m.Uid).Contains(l.ManagerAssigned ?? 0))
                    .ToList();

                var leadStatusCounts = allLeads
                    .GroupBy(l => l.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                int totalLeads = allLeads.Count;
                int newLeads = leadStatusCounts.GetValueOrDefault("New", 0);
                int contactedLeads = leadStatusCounts.GetValueOrDefault("Contacted", 0);
                int followUpLeads = leadStatusCounts.GetValueOrDefault("Follow-up", 0);
                int convertedLeads = leadStatusCounts.GetValueOrDefault("Converted", 0);
                int lostLeads = leadStatusCounts.GetValueOrDefault("Lost", 0);

                var leadList = allLeads
                    .Select(l => new {
                        l.Lid,
                        l.Name,
                        l.Email,
                        l.Phone,
                        l.Status,
                        ManagerAssigned = l.ManagerAssigned
                    })
                    .OrderBy(l => l.ManagerAssigned == null ? 0 : 1)
                    .ThenBy(l => l.Lid)
                    .ToList();

                return Ok(new {
                    totalLeads,
                    newLeads,
                    contactedLeads,
                    followUpLeads,
                    convertedLeads,
                    lostLeads,
                    leadStatusCounts,
                    leadList
                });
            }
        }
    }
}
