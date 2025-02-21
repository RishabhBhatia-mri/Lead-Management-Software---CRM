using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using CsvHelper;
using LeadManagment.Models;
using System.Collections.Generic;
using System.Formats.Asn1;

namespace LeadManagment.Leads {
    [Route("manager-leads")]
    [ApiController]
    [Authorize(Roles = "Manager,Sales Representative")] // Only managers can access this
    public class ManagerLeadsController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ManagerLeadsController(LeadsManagementContext context) {
            _context = context;
        }

        [HttpPost("addNewLeads")]
        public IActionResult AddNewLead([FromBody] LeadRequest request) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRoleClaim)) {
                return Unauthorized(new { message = "Invalid token. Role missing." });
            }

            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.Source)) {
                return BadRequest(new { message = "Name, Email, Phone, and Source are required fields." });
            }

            var validSources = new[] { "Website", "Reference", "Ads", "Social Media" };
            if (!validSources.Contains(request.Source)) {
                return BadRequest(new { message = "Invalid source." });
            }

            var validStatuses = new[] { "New", "Contacted", "Follow-up", "Converted", "Lost" };
            string status = string.IsNullOrEmpty(request.Status) ? "New" : request.Status;
            if (!validStatuses.Contains(status)) {
                return BadRequest(new { message = "Invalid status." });
            }

            // Determine Manager Assigned
            int managerAssignedId = userId; // Default to the creator (if Manager)

            if (userRoleClaim == "Sales Representative") {
                var salesRep = _context.Users.FirstOrDefault(u => u.Uid == userId);
                if (salesRep == null || salesRep.ReportsTo == null) {
                    return BadRequest(new { message = "Sales Representative must have a manager assigned." });
                }

                managerAssignedId = salesRep.ReportsTo.Value; // Assign to the Sales Rep's Manager
            }

            // Ensure SalesRepAssigned is either NULL or a valid integer
            int? salesRepId = request.SalesRepAssigned.HasValue && request.SalesRepAssigned > 0
                ? request.SalesRepAssigned
                : null;

            var newLead = new Lead {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Source = request.Source,
                Status = status,
                ManagerAssigned = managerAssignedId, // Assign the correct Manager
                SalesRepAssigned = salesRepId,
                CreatedBy = userId, // The user creating the lead
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Leads.Add(newLead);
            _context.SaveChanges();

            return Ok(new { message = "Lead added successfully!" });
        }


        [HttpGet("getSalesReps")]
        public IActionResult GetSalesReps() {
            // Get manager ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Fetch sales reps under the logged-in manager
            var salesReps = _context.Users
                .Where(u => u.ReportsTo == managerId)
                .Select(u => new {
                    Uid = u.Uid,
                    Name = u.Name
                })
                .ToList();

            if (salesReps.Count == 0) {
                return NotFound(new { message = "No sales representatives found under your management." });
            }

            return Ok(salesReps);
        }


        [HttpPost("addCsv")]
        public IActionResult ImportLeads(IFormFile file) {
            // Get manager ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId)) {
                return Unauthorized(new { message = "Invalid token. User ID missing." });
            }

            // Validate file
            if (file == null || file.Length == 0) {
                return BadRequest(new { message = "File is required." });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".csv" && fileExtension != ".xls" && fileExtension != ".xlsx") {
                return BadRequest(new { message = "Invalid file format. Only .csv, .xls, and .xlsx are allowed." });
            }

            var leadsToInsert = new List<Lead>();
            var validSources = new[] { "Website", "Reference", "Ads", "Social Media" };
            var validStatuses = new[] { "New", "Contacted", "Follow-up", "Converted", "Lost" };

            try {
                using (var stream = new MemoryStream()) {
                    file.CopyTo(stream);
                    stream.Position = 0;

                    if (fileExtension == ".csv") {
                        using (var reader = new StreamReader(stream))
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                            var records = csv.GetRecords<LeadCsvModel>().ToList();

                            foreach (var record in records) {
                                if (string.IsNullOrWhiteSpace(record.Name) ||
                                    string.IsNullOrWhiteSpace(record.Phone) ||
                                    !validSources.Contains(record.Source)) {
                                    continue; // Skip invalid rows
                                }

                                int? salesRepId = record.SalesRepAssigned;
                                if (salesRepId.HasValue) {
                                    bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == salesRepId && u.ReportsTo == managerId);
                                    if (!isSalesRepUnderManager) {
                                        salesRepId = null; // Ignore invalid sales rep assignments
                                    }
                                }

                                var newLead = new Lead {
                                    Name = record.Name,
                                    Email = record.Email,
                                    Phone = record.Phone,
                                    Source = record.Source,
                                    Status = validStatuses.Contains(record.Status) ? record.Status : "New",
                                    ManagerAssigned = managerId,
                                    SalesRepAssigned = salesRepId,
                                    CreatedBy = managerId
                                };
                                leadsToInsert.Add(newLead);
                            }
                        }
                    } else // Excel file handling
                      {
                        using (var package = new ExcelPackage(stream)) {
                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row <= rowCount; row++) {
                                var name = worksheet.Cells[row, 2].Text.Trim();
                                var email = worksheet.Cells[row, 3].Text.Trim();
                                var phone = worksheet.Cells[row, 4].Text.Trim();
                                var source = worksheet.Cells[row, 5].Text.Trim();
                                var status = worksheet.Cells[row, 6].Text.Trim();
                                var salesRepIdText = worksheet.Cells[row, 8].Text.Trim();

                                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) || !validSources.Contains(source)) {
                                    continue; // Skip invalid rows
                                }

                                int? salesRepId = null;
                                if (int.TryParse(salesRepIdText, out int parsedSalesRepId)) {
                                    bool isSalesRepUnderManager = _context.Users.Any(u => u.Uid == parsedSalesRepId && u.ReportsTo == managerId);
                                    if (isSalesRepUnderManager) {
                                        salesRepId = parsedSalesRepId;
                                    }
                                }

                                var newLead = new Lead {
                                    Name = name,
                                    Email = email,
                                    Phone = phone,
                                    Source = source,
                                    Status = validStatuses.Contains(status) ? status : "New",
                                    ManagerAssigned = managerId,
                                    SalesRepAssigned = salesRepId,
                                    CreatedBy = managerId
                                };
                                leadsToInsert.Add(newLead);
                            }
                        }
                    }
                }
                if (leadsToInsert.Count > 0) {
                    _context.Leads.AddRange(leadsToInsert);
                    _context.SaveChanges();

                    // ✅ Insert record into `import_export_logs`
                    var log = new ImportExportLog {
                        Uid = managerId,
                        ActionType = "Imported",
                        FileName = file.FileName,
                        LogTimestamp = DateTime.UtcNow
                    };

                    _context.ImportExportLogs.Add(log);
                    _context.SaveChanges();

                    int logId = log.LogId; // Get newly created LogId

                    // ✅ Insert records into `lead_import_log`
                    var importLogs = leadsToInsert.Select(lead => new LeadImportLog {
                        LogId = logId,
                        Lid = lead.Lid,
                        ImportedBy = managerId,
                        ImportedAt = DateTime.UtcNow
                    }).ToList();

                    _context.LeadImportLogs.AddRange(importLogs);
                    _context.SaveChanges();
                

                return Ok(new { message = $"{leadsToInsert.Count} leads imported successfully and logged." });
                } else {
                    return BadRequest(new { message = "No valid leads found in the file." });
                }
            } catch (Exception ex) {
                return BadRequest(new { message = "Error processing file", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

    }

    // DTO for Request Body
    public class LeadRequest {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Source { get; set; }
        public string Status { get; set; } // Optional, defaults to "New"
        public int? SalesRepAssigned { get; set; } // Optional
    }
}
