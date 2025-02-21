using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LeadManagement.Controllers {
    [Route("viewLeads")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager,Sales Representative")]
    public class ViewLeadsController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ViewLeadsController(LeadsManagementContext context) {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLeads() {
            // Get the logged-in user's role and ID
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value); // Assumes NameIdentifier stores UId

            IQueryable<dynamic> leadsQuery;

            if (userRole == "Admin") {
                // Admin can see all leads
                leadsQuery = _context.Leads
                    .Select(l => new {
                        id = l.Lid,
                        Name = l.Name,
                        Email = l.Email,
                        Phone = l.Phone,
                        Status = l.Status,
                        LeadSource = l.Source,
                        AssignedTo = _context.Users
                            .Where(u => u.Uid == l.SalesRepAssigned)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unassigned"
                    });
            } else if (userRole == "Manager") {
                // Manager can only see leads assigned to their Sales Reps
                leadsQuery = _context.Leads
                    .Where(l => _context.Users.Any(u => u.Uid == l.SalesRepAssigned && u.ReportsTo == userId))
                    .Select(l => new {
                        id = l.Lid,
                        Name = l.Name,
                        Email = l.Email,
                        Phone = l.Phone,
                        Status = l.Status,
                        LeadSource = l.Source,
                        AssignedTo = _context.Users
                            .Where(u => u.Uid == l.SalesRepAssigned)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unassigned"
                    });
            } else {
                // Sales Reps can only see leads assigned to them
                leadsQuery = _context.Leads
                    .Where(l => l.SalesRepAssigned == userId)
                    .Select(l => new {
                        id = l.Lid,
                        Name = l.Name,
                        Email = l.Email,
                        Phone = l.Phone,
                        Status = l.Status,
                        LeadSource = l.Source
                    });
            }

            var leads = await leadsQuery.ToListAsync();
            return Ok(leads);
        }
    }
}
