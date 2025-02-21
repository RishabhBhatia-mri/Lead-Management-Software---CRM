using LeadManagement.Services;
using LeadManagment.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LeadManagement.Controllers {
    [Route("admin")]
    [ApiController]
    public class AdminController : ControllerBase {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService) {
            _adminService = adminService;
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) {
            User newUser = new User {
                Name = request.Name,
                Email = request.Email,
                Role = request.Role,
                PhoneNo = request.PhoneNo,
                ReportsTo = request.ReportsTo ?? 1  // Default to Admin if Manager
            };

            return await _adminService.CreateUserAsync(newUser);
        }


        // Fetch managers list
        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers() {
            var managers = await _adminService.GetManagersAsync();
            return Ok(managers);
        }

        public class CreateUserRequest {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public string PhoneNo { get; set; }
            public int? ReportsTo { get; set; }  // Nullable because Manager defaults to Admin
        }

    }
}
