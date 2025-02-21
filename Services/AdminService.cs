using LeadManagment.Models;
using LeadManagement.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LeadManagement.Services {
    public class AdminService {
        private readonly LeadsManagementContext _context;
        private readonly EmailService _emailService;

        public AdminService(LeadsManagementContext context, EmailService emailService) {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> CreateUserAsync(User newUser) {
            if (await _context.Users.AnyAsync(u => u.Email == newUser.Email)) {
                return new BadRequestObjectResult(new { message = "Email already exists." });
            }

            // Prevent direct Admin account creation
            if (newUser.Role == "Admin") {
                return new BadRequestObjectResult(new { message = "You are not allowed to create an Admin account." });
            }

            // Generate a secure password
            string temporaryPassword = PasswordHelper.GenerateTemporaryPassword();
            newUser.Password = PasswordHelper.HashPassword(temporaryPassword);
            newUser.DateOfJoining = DateTime.UtcNow;
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            // Set ReportsTo (Default to Admin if Manager, otherwise use the provided ReportsTo value)
            newUser.ReportsTo = (newUser.Role == "Manager") ? 1 : newUser.ReportsTo;

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Send temporary password via email
            string emailBody = $@"
            <h3>Welcome, {newUser.Name}!</h3>
            <p>Your account has been created successfully.</p>
            <p><b>Email:</b> {newUser.Email}</p>
            <p><b>Temporary Password:</b> {temporaryPassword}</p>
            <p>Please change your password after logging in.</p>";

            _emailService.SendEmail(newUser.Email, "Account Created", emailBody);

            return new OkObjectResult(new { message = "User created successfully, email sent." });
        }

        public async Task<List<ManagerDto>> GetManagersAsync() {
            return await _context.Users
                .Where(u => u.Role == "Manager")
                .Select(u => new ManagerDto {
                    Uid = u.Uid,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNo = u.PhoneNo,
                    SalesRepCount = _context.Users.Count(sr => sr.ReportsTo == u.Uid && sr.Role == "Sales Representative")
                })
                .ToListAsync();
        }
        public class ManagerDto {
            public int Uid { get; set; }
            public string Name { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string PhoneNo { get; set; } = null!;
            public int SalesRepCount { get; set; }
        }

    }
}
