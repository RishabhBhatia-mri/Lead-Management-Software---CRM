using System.Security.Claims;
using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeadManagment.Dashboards {
    [Route("dashboard/manager")]  // Changed route
    [ApiController]
    [Authorize(Roles = "Manager")] // Ensures only managers can access
    public class ManagerDashboard : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ManagerDashboard(LeadsManagementContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }



        [HttpGet("count")]
        public IActionResult GetCount() {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            var allLeads = _context.Leads
                .Where(l => l.ManagerAssigned == managerId)
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

            return Ok(new {
                totalLeads,
                newLeads,
                contactedLeads,
                followUpLeads,
                convertedLeads,
                lostLeads,
                leadStatusCounts
            });
        }


        [HttpGet("sales-reps/details")]
        public IActionResult GetContent(int managerId) {
            if (User == null) {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            var salesReps = _context.Users
                    .Where(u => u.ReportsTo == managerId)
                    .Select(u => new {
                        u.Uid,
                        u.Name
                    })
                    .ToList();

            if (!salesReps.Any()) {
                return Ok(new { message = "No sales representatives found under this manager.", salesReps = new List<object>() });
            }


            var salesRepsWithLeads = salesReps.Select(salesRep => new {
                salesRep.Uid,
                salesRep.Name,
                LeadsAssigned = _context.Leads.Count(l => l.SalesRepAssigned == salesRep.Uid),
                FollowUpLeads = _context.Leads.Count(l => l.SalesRepAssigned == salesRep.Uid && l.Status == "Follow-up"),
                ConvertedLeads = _context.Leads.Count(l => l.SalesRepAssigned == salesRep.Uid && l.Status == "Converted")
            }).ToList();

            return Ok(new {
                message = "Sales Representatives detailed data fetched successfully",
                totalSalesReps = salesReps.Count,
                salesReps = salesRepsWithLeads
            });
        }



        [HttpGet("assignment")]
        public IActionResult GetLeadAssignments() {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Filter leads based on the manager's ID
            var allLeads = _context.Leads
                .Where(l => l.ManagerAssigned == managerId) // 🔹 Ensure only manager's leads are selected
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

            // Filter the leadList to include only leads under the logged-in manager
            var leadList = _context.Leads
                .Where(l => l.ManagerAssigned == managerId) // 🔹 Filtering leads by manager
                .Select(l => new {
                    l.Lid,
                    l.Name,
                    l.Email,
                    l.Phone,
                    l.Status,
                    SalesRep_Assigned = l.SalesRepAssigned != null
                        ? _context.Users.Where(u => u.Uid == l.SalesRepAssigned).Select(u => u.Name).FirstOrDefault()
                        : "Unassigned"
                })
                .OrderBy(l => l.SalesRep_Assigned == "Unassigned" ? 0 : 1)
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
