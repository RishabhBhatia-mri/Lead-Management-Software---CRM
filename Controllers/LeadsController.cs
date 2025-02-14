using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeadManagment.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OfficeOpenXml; // For Excel Processing
using System.Globalization;
using System.Text;

[Route("leads")]
[ApiController]
[Authorize]
public class LeadController : ControllerBase
{
    private readonly LeadsManagementContext _context;
    public LeadController(LeadsManagementContext context)
    {
        _context = context;
    }

    // GET /leads - Fetch leads based on role
    [HttpGet]
    public async Task<IActionResult> GetLeads()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userRole = User.FindFirst(ClaimTypes.Role).Value;

        var query = _context.Leads.AsQueryable();
        if (userRole == "Sales Representative")
        {
            query = query.Where(l => l.AssignedTo == userId);
        }
        else if (userRole == "Manager")
        {
            var salesRepIds = await _context.Users.Where(u => u.ReportsTo == userId).Select(u => u.Uid).ToListAsync();
            query = query.Where(l => salesRepIds.Contains(l.AssignedTo ?? 0));
        }
        return Ok(await query.ToListAsync());
    }

    // POST /leads - Create a new lead
    [HttpPost("create")]
    public async Task<IActionResult> CreateLead([FromBody] Lead lead)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userRole = User.FindFirst(ClaimTypes.Role).Value;

        if (userRole == "Sales Representative")
        {
            var existingLeadCount = await _context.Leads.CountAsync(l => l.CreatedBy == userId);
            if (existingLeadCount >= 1)
                return BadRequest(new { message = "Sales Representatives can only create one lead." });

            lead.AssignedTo = null; // Sales reps can't assign leads
        }

        lead.CreatedBy = userId;
        lead.CreatedAt = DateTime.UtcNow;
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();
        return Ok(lead);
    }

    // POST /leads/import - Bulk lead import via Excel/CSV
    [HttpPost("import")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ImportLeads([FromForm] IFormFile file)
    {
        var userId = int.Parse(User.Claims.First(c => c.Type == "Uid").Value);
        var userRole = User.Claims.First(c => c.Type == "Role").Value;

        if (userRole == "SalesRepresentative")
        {
            return Forbid("Sales Representatives are not allowed to upload leads.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        string fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (fileExtension != ".xlsx" && fileExtension != ".csv")
        {
            return BadRequest("Only .xlsx or .csv files are allowed.");
        }

        var leads = new List<Lead>();

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (fileExtension == ".xlsx")
            {
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        leads.Add(new Lead
                        {
                            Name = worksheet.Cells[row, 1].Text,
                            Email = worksheet.Cells[row, 2].Text,
                            Phone = worksheet.Cells[row, 3].Text,
                            Source = worksheet.Cells[row, 4].Text,
                            Status = "New",
                            AssignedTo = null, // Bulk-imported leads are unassigned
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else if (fileExtension == ".csv")
            {
                using (var reader = new StreamReader(stream))
                {
                    var headerSkipped = false;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!headerSkipped) { headerSkipped = true; continue; } // Skip header row

                        var values = line.Split(',');

                        leads.Add(new Lead
                        {
                            Name = values[0],
                            Email = values[1],
                            Phone = values[2],
                            Source = values[3],
                            Status = "New",
                            AssignedTo = null,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        if (leads.Count == 0)
        {
            return BadRequest("No valid leads found in the file.");
        }

        // Save import log
        var importLog = new ImportExportLog
        {
            Uid = userId,
            ActionType = "Import",
            FileName = file.FileName,
            LogTimestamp = DateTime.UtcNow
        };

        _context.ImportExportLogs.Add(importLog);
        await _context.SaveChangesAsync();

        // Insert Leads and track imports
        foreach (var lead in leads)
        {
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            _context.LeadImportLogs.Add(new LeadImportLog
            {
                LogId = importLog.LogId,
                Lid = lead.Lid,
                ImportedBy = userId,
                ImportedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"{leads.Count} leads imported successfully.", file = file.FileName });
    }
}
