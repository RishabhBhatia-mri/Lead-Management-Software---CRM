using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeadManagment.Controllers
{
    [Route("admin/user-management")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly LeadsManagementContext _context;

        public UserManagementController(LeadsManagementContext context)
        {
            _context = context;
        }

        // GET: api/User
        [HttpGet]
        [Authorize(Roles = "Admin")] // Only Admin can access this endpoint
        public async Task<IActionResult> GetAllUsers()
        {
            if (!User.IsInRole("Admin"))
            {
                return Unauthorized(new { message = "Access denied.Only Admin can access this resource." });
            }
            var users = await _context.Users
                .Select(user => new
                {
                    user.Uid,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.PhoneNo
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
