using BCrypt.Net; // Add this namespace
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using LeadManagment.Models;
using System;

namespace LeadManagment.Controllers {
    [Route("profile")]
    [ApiController]
    [Authorize] 
    public class ProfileController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ProfileController(LeadsManagementContext context) {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetProfile() {
            // Extract user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Fetch user details from the database
            var user = _context.Users.FirstOrDefault(u => u.Uid == userId);
            if (user == null) {
                return NotFound(new { message = "User not found." });
            }

            // Get the name of the reporting manager (if ReportsTo is not null)
            string? reportsToName = null;
            if (user.ReportsTo.HasValue) {
                reportsToName = _context.Users
                    .Where(u => u.Uid == user.ReportsTo.Value)
                    .Select(u => u.Name)
                    .FirstOrDefault();
            }

            // Return user details without exposing the password
            return Ok(new {
                user.Name,
                user.Email,
                user.Role,
                user.PhoneNo,
                user.DateOfJoining,
                user.CreatedAt,
                user.UpdatedAt,
                ReportsTo = reportsToName // Returning name instead of ID
            });
        }


        [HttpPost("update")]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request) {
            // Extract the user ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if the userIdClaim is not null or empty and if it can be parsed to an integer
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Retrieve the user from the database using the parsed user ID
            var user = _context.Users.FirstOrDefault(u => u.Uid == userId);
            if (user == null) {
                return NotFound(new { message = "User not found." });
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword)) {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.Password = hashedPassword;
                Console.WriteLine($"Password updated: {user.Password}");
            }


            // Set the `UpdatedAt` timestamp
            user.UpdatedAt = DateTime.UtcNow;

            // Save changes to the database
            _context.SaveChanges();

            return Ok(new { message = "Profile updated successfully." });
        }

        public class UpdateProfileRequest {
            public string? NewPassword { get; set; }
            public string? PhoneNo { get; set; }
        }
    }
}
