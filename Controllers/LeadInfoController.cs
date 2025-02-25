using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LeadManagment.Models;

namespace LeadManagement.Controllers {
    [Route("leadInfo")]
    [ApiController]
    [Authorize] // Ensure only authorized users can access this API
    public class LeadInfoController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public LeadInfoController(LeadsManagementContext context) {
            _context = context;
        }

        // GET: leadInfo/{LId}
        [HttpGet("{LId}")]
        public async Task<IActionResult> GetLeadInfo(int LId) {
            var lead = await _context.Leads
                .Include(l => l.SalesRepAssignedNavigation) // Fetch assigned sales rep details
                .Include(l => l.ManagerAssignedNavigation)  // Fetch assigned manager details
                .Include(l => l.CreatedByNavigation)        // Fetch creator details
                .FirstOrDefaultAsync(l => l.Lid == LId);

            if (lead == null) {
                return NotFound(new { message = "Lead not found" });
            }

            // Fetch follow-up notes related to this lead (LId)
            var followUps = await _context.LeadFollowUps
                .Where(f => f.Lid == LId)
                .Select(f => new {
                    f.Fid,
                    f.FollowUpDate,
                    f.Status,
                    f.Notes,
                    AddedBy = _context.Users.Where(u => u.Uid == f.Uid).Select(u => u.Name).FirstOrDefault()
                })
                .ToListAsync();

            var leadDetails = new {
                LeadId = lead.Lid,
                Name = lead.Name,
                Email = lead.Email,
                Phone = lead.Phone,
                LeadSource = lead.Source,
                Status = lead.Status,
                AssignedTo = lead.SalesRepAssignedNavigation?.Name ?? "Unassigned", 
                LastContacted = lead.UpdatedAt?.ToString("yyyy-MM-dd"),

                // Detailed Info
                FullName = lead.Name,
                Location = "INDIA", 
                PriorityLevel = "High", 
                AddedBy = new {
                    Name = lead.CreatedByNavigation?.Name ?? "Unknown",
                    Date = lead.CreatedAt?.ToString("yyyy-MM-dd hh:mm tt")
                },
                ModifiedBy = new {
                    Name = lead.SalesRepAssignedNavigation?.Name ?? "Unknown", 
                    Date = lead.UpdatedAt?.ToString("yyyy-MM-dd hh:mm tt")
                },
                Description = "Lead interested in a 2-bedroom apartment.", 
                FollowUpNotes = followUps 
            };

            return Ok(leadDetails);
        }
        [HttpDelete("{LId}")]
        public async Task<IActionResult> DeleteLead(int LId) {
            var lead = await _context.Leads.FindAsync(LId);

            if (lead == null) {
                return NotFound(new { message = "Lead not found" });
            }

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lead deleted successfully" });
        }
    }
}
