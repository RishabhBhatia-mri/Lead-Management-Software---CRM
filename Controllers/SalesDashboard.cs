using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LeadManagment.Controllers {
    [Route("dashboard/sales")]
    [ApiController]
    [Authorize(Roles = "Sales Representative")]  // Ensure only sales reps can access
    public class SalesDashboard : ControllerBase {
        private readonly LeadsManagementContext _context;

        public SalesDashboard(LeadsManagementContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetSalesRepLeadsCountAPI() {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId)) {
                return Unauthorized(new { message = "Invalid token. Sales Rep ID missing." });
            }

            var result = await GetSalesRepLeadsCount(salesRepId);
            return Ok(result);
        }

        // New method that can be called directly from DashboardController
        public async Task<object> GetSalesRepLeadsCount(int salesRepId) {
            var leadStatusCounts = await _context.Leads
                .Where(l => l.SalesRepAssigned == salesRepId) // Ensure correct column name
                .GroupBy(l => l.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return new {
                totalLeads = leadStatusCounts.Values.Sum(),
                newLeads = leadStatusCounts.GetValueOrDefault("New", 0),
                contactedLeads = leadStatusCounts.GetValueOrDefault("Contacted", 0),
                followUpLeads = leadStatusCounts.GetValueOrDefault("Follow-up", 0),
                convertedLeads = leadStatusCounts.GetValueOrDefault("Converted", 0),
                lostLeads = leadStatusCounts.GetValueOrDefault("Lost", 0)
            };
        }
    }
}
