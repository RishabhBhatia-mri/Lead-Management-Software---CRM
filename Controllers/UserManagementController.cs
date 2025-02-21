using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeadManagment.Controllers {
    [Route("user-management")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class UserManagementController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public UserManagementController(LeadsManagementContext context) {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers() {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            IQueryable<User> query = _context.Users;

            if (role == "Manager") {
                // Filter only Sales Reps under the Manager
                query = query.Where(user => user.ReportsTo == userId && user.Role == "Sales Representative");
            }

            var users = await query
                .Select(user => new {
                    user.Uid,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.PhoneNo,
                    DateOfJoining = user.DateOfJoining.ToString("dd-MM-yyyy") // Format date
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
