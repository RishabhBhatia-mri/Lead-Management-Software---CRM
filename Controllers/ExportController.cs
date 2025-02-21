using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LeadManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // For Excel export
using iTextSharp.text;
using iTextSharp.text.pdf; // For PDF export
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LeadManagement.Controllers {
    [Route("export")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only Admin users can access
    public class ExportController : ControllerBase {
        private readonly LeadsManagementContext _context;

        public ExportController(LeadsManagementContext context) {
            _context = context;
        }

        // Export leads with additional filters like SalesRep, Status, Date, etc.
        [HttpPost("leads")]
        public async Task<IActionResult> ExportLeads([FromBody] ExportRequest request) {
            var query = _context.Leads
                .Include(l => l.ManagerAssignedNavigation)  // Include the related Manager
                .Include(l => l.SalesRepAssignedNavigation) // Include the related Sales Rep
                .AsQueryable();

            // Filter by Manager if provided
            if (request.ManagerId.HasValue) {
                query = query.Where(l => l.ManagerAssigned == request.ManagerId.Value);
            }

            // Filter by SalesRep if provided
            if (request.SalesRepId.HasValue) {
                query = query.Where(l => l.SalesRepAssigned == request.SalesRepId.Value);
            }

            // Apply Status filter only if provided
            if (!string.IsNullOrEmpty(request.Status)) {
                query = query.Where(l => l.Status == request.Status);
            }

            // Filter by Date range if provided
            if (request.StartDate.HasValue && request.EndDate.HasValue) {
                query = query.Where(l => l.CreatedAt >= request.StartDate && l.CreatedAt <= request.EndDate);
            }

            var leads = await query.ToListAsync();

            // Determine the export format
            if (request.Format.ToLower() == "excel") {
                return ExportToExcel(leads);
            } else if (request.Format.ToLower() == "pdf") {
                return ExportToPdf(leads);
            }

            return BadRequest("Invalid format. Supported formats are Excel and PDF.");
        }


        private IActionResult ExportToExcel(List<Lead> leads) {
            using (var package = new ExcelPackage()) {
                var worksheet = package.Workbook.Worksheets.Add("Leads");

                worksheet.Cells[1, 1].Value = "Lead ID";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Phone";
                worksheet.Cells[1, 5].Value = "Status";
                worksheet.Cells[1, 6].Value = "Sales Rep Assigned";

                var row = 2;
                foreach (var lead in leads) {
                    worksheet.Cells[row, 1].Value = lead.Lid;
                    worksheet.Cells[row, 2].Value = lead.Name;
                    worksheet.Cells[row, 3].Value = lead.Email;
                    worksheet.Cells[row, 4].Value = lead.Phone;
                    worksheet.Cells[row, 5].Value = lead.Status;
                    worksheet.Cells[row, 6].Value = lead.SalesRepAssignedNavigation != null
                        ? lead.SalesRepAssignedNavigation.Name
                        : "Not Assigned";
                    row++;
                }

                // Create a file name based on current date and time
                var fileName = $"LeadsExport_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.xlsx";

                // Get the path to the Downloads folder using a more robust method
                var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                // Ensure the directory exists
                if (!Directory.Exists(downloadsFolder)) {
                    Directory.CreateDirectory(downloadsFolder);
                }

                // Define the full path to save the file
                var filePath = Path.Combine(downloadsFolder, fileName);

                // Write the Excel file to the defined path
                System.IO.File.WriteAllBytes(filePath, package.GetAsByteArray());

                // Save log to import_export_logs
                SaveExportLog(fileName, "Excel");

                // Return the file as a downloadable response
                return PhysicalFile(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private IActionResult ExportToPdf(List<Lead> leads) {
            try {
                using (var ms = new MemoryStream()) {
                    var doc = new Document();
                    var pdfWriter = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    doc.Add(new Paragraph("Leads Export"));
                    doc.Add(new Paragraph("\n"));

                    var table = new PdfPTable(6) { WidthPercentage = 100 };

                    var headers = new[] { "Lead ID", "Name", "Email", "Phone", "Status", "Sales Rep Assigned" };
                    foreach (var header in headers) {
                        var cell = new PdfPCell(new Phrase(header)) {
                            BackgroundColor = BaseColor.LIGHT_GRAY,
                            HorizontalAlignment = Element.ALIGN_CENTER
                        };
                        table.AddCell(cell);
                    }

                    foreach (var lead in leads) {
                        table.AddCell(lead.Lid.ToString());
                        table.AddCell(lead.Name);
                        table.AddCell(lead.Email);
                        table.AddCell(lead.Phone);
                        table.AddCell(lead.Status);
                        table.AddCell(lead.SalesRepAssignedNavigation != null
                            ? lead.SalesRepAssignedNavigation.Name
                            : "Not Assigned");
                    }

                    doc.Add(table);
                    doc.Close();

                    var fileName = $"LeadsExport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
                    System.IO.File.WriteAllBytes(downloadsPath, ms.ToArray());

                    SaveExportLog(fileName, "PDF");

                    if (!System.IO.File.Exists(downloadsPath)) {
                        return StatusCode(500, "Error generating PDF file.");
                    }

                    return PhysicalFile(downloadsPath, "application/pdf", fileName);

                }
            } catch (Exception ex) {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private void SaveExportLog(string fileName, string format) {
            var exportLog = new ImportExportLog {
                Uid = 1, // Assuming the admin's UID is 1, replace with actual admin UID
                ActionType = "Exported",
                FileName = fileName, // Make sure the correct file name is used
                LogTimestamp = DateTime.Now,
                Format = format,
                ExportDate = DateTime.Now
            };

            _context.ImportExportLogs.Add(exportLog);
            _context.SaveChanges();
        }
    }

    // Request model for dynamic export filters
    public class ExportRequest {
        public int? ManagerId { get; set; }
        public int? SalesRepId { get; set; }
        public string Status { get; set; } // Optional status field
        public DateTime? StartDate { get; set; } // Start Date filter
        public DateTime? EndDate { get; set; } // End Date filter
        public string Format { get; set; } // Excel or PDF
    }

}
