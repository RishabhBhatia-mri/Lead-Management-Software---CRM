using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System;
using LeadManagment.Models;

namespace LeadManagement.Controllers {
    [Route("leads")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager,Sales Representative")]
    public class LeadsController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public LeadsController(LeadsManagementContext context) {
            _context = context;
        }

        // Update Phone Number
        [HttpPatch("update-phone/{lid}")]
        public IActionResult UpdatePhone(int lid, [FromBody] UpdatePhoneRequest request) {
            Console.WriteLine($"Received Update Request: LeadId={lid}, NewPhone={request.NewPhone}");

            if (string.IsNullOrWhiteSpace(request.NewPhone) || request.NewPhone.Length != 10 || !request.NewPhone.All(char.IsDigit)) {
                return BadRequest(new { message = "Phone number must be exactly 10 digits." });
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var lead = _context.Leads.FirstOrDefault(l => l.Lid == lid);
            if (lead == null) return NotFound(new { message = "Lead not found." });

            if (userRole == "Sales Representative" && lead.SalesRepAssigned != userId) {
                return Unauthorized(new { message = "You can only update leads assigned to you." });
            }

            string oldPhone = lead.Phone;
            lead.Phone = request.NewPhone;

            _context.LeadUpdateLogs.Add(new LeadUpdateLog {
                Lid = lid,
                Uid = userId,
                FieldUpdated = "Phone",
                OldValue = oldPhone,
                NewValue = request.NewPhone,
                UpdatedAt = DateTime.UtcNow
            });

            _context.SaveChanges();
            return Ok(new { message = "Phone number updated successfully." });
        }




        // Update Lead Status
        [HttpPatch("update-status/{lid}")]
        public IActionResult UpdateStatus(int lid, [FromBody] UpdateStatusRequest request) {
            Console.WriteLine($"Received status update request: LeadId={lid}, NewStatus={request.NewStatus}");

            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var lead = _context.Leads.FirstOrDefault(l => l.Lid == lid);
            if (lead == null) return NotFound(new { message = "Lead not found." });

            if (userRole == "Sales Representative" && lead.SalesRepAssigned != userId) {
                return Unauthorized(new { message = "You can only update leads assigned to you." });
            }

            string oldStatus = lead.Status;
            lead.Status = request.NewStatus;

            _context.LeadStatusHistories.Add(new LeadStatusHistory {
                Lid = lid,
                Uid = userId,
                OldStatus = oldStatus,
                NewStatus = request.NewStatus,
                TimeOfChange = DateTime.UtcNow
            });

            _context.SaveChanges();
            return Ok(new { message = "Lead status updated successfully." });
        }


        // Add a Note
        [HttpPost("add-note/{lid}")]
        public IActionResult AddNote(int lid, [FromBody] AddNoteRequest request) {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            _context.LeadActivityLogs.Add(new LeadActivityLog {
                Lid = lid,
                Uid = userId,
                ActivityDate = DateTime.UtcNow,
                Notes = request.Notes,
                Responded = request.Responded
            });
            _context.SaveChanges();

            return Ok(new { message = "Note added successfully." });
        }

        // Add Follow-up
        [HttpPost("add-followup/{lid}")]
        public IActionResult AddFollowUp(int lid, [FromBody] AddFollowUpRequest request) {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            string trimmedStatus = request.Status?.Trim();  // Trim spaces

            // Ensure status is valid (Matching ENUM values in DB)
            var validStatuses = new List<string> { "Pending", "Completed", "Missed" };
            if (!validStatuses.Contains(trimmedStatus)) {
                return BadRequest(new { message = "Invalid status value. Allowed values: Pending, Completed, Missed." });
            }

            _context.LeadFollowUps.Add(new LeadFollowUp {
                Lid = lid,
                Uid = userId,
                FollowUpDate = request.FollowUpDate,
                Status = trimmedStatus,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            });

            _context.SaveChanges();
            return Ok(new { message = "Follow-up added successfully." });
        }

        // Get Lead Details (Only if assigned to the Sales Rep)
        [HttpGet("{lid}")]
        public IActionResult GetLeadDetails(int lid) {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            // Fetch lead only if assigned to the logged-in Sales Rep
            var lead = _context.Leads
                .Where(l => l.Lid == lid && l.SalesRepAssigned == userId)
                .Select(l => new {
                    l.Lid,
                    l.Name,
                    l.Email,
                    l.Phone,
                    l.Status,
                    AssignedTo = l.SalesRepAssigned
                })
                .FirstOrDefault();

            if (lead == null) return NotFound(new { message = "Lead not found or not assigned to you." });

            return Ok(lead);
        }


        private int? GetUserId() {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : (int?)null;
        }

        public class UpdatePhoneRequest {
            public string NewPhone { get; set; }
        }

        public class UpdateStatusRequest {
            public string NewStatus { get; set; }
        }

        public class AddNoteRequest {
            public string Notes { get; set; }
            public bool Responded { get; set; }
        }

        public class AddFollowUpRequest {
            public DateTime FollowUpDate { get; set; }
            public string Status { get; set; }
            public string Notes { get; set; }
        }
    }
}
