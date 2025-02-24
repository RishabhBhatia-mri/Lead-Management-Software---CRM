using LeadManagment.Controllers;
using LeadManagment.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace LeadManagement.Tests
{
    public class LeadControllerTests
    {
        private readonly LeadsManagementContext _context;
        private readonly LeadController _controller;

        public LeadControllerTests()
        {
            var options = new DbContextOptionsBuilder<LeadsManagementContext>()
                .UseMySql("server=localhost;database=leads_management;uid=root;pwd=smit@123",
                          new MySqlServerVersion(new Version(8, 0, 41)))
                .Options;

            _context = new LeadsManagementContext(options);
            _controller = new LeadController(_context);
            InitializeExistingLead();
        }
        private void InitializeExistingLead()
        {
            var existingLead = _context.Leads.FirstOrDefault();
            if (existingLead == null)
            {
                throw new System.Exception("No leads exist in the database. Please add test data.");
            }
        }

        private void SetUserContext(string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetLeads_AsAdmin_ReturnsAllLeads()
        {
            // Arrange
            string adminId = "1";
            SetUserContext(adminId, "Admin");

            // Act
            var result = await _controller.GetLeads();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadsProperty = responseValue.GetType().GetProperty("leads");
            var leads = leadsProperty?.GetValue(responseValue) as IEnumerable<Lead>;

            Assert.NotNull(leads);

            var expectedLeads = _context.Leads.ToList();
            Assert.Equal(expectedLeads.Count(), leads.Count());
        }

        [Fact]
        public async Task GetLeads_AsManager_ReturnsAssignedLeads()
        {
            // Arrange
            string managerId = "2";
            SetUserContext(managerId, "Manager");

            // Act
            var result = await _controller.GetLeads();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadsProperty = responseValue.GetType().GetProperty("leads");
            var leads = leadsProperty?.GetValue(responseValue) as IEnumerable<Lead>;

            Assert.NotNull(leads);

            var expectedLeads = _context.Leads.Where(l => l.ManagerAssigned == int.Parse(managerId)).ToList();
            Assert.Equal(expectedLeads.Count(), leads.Count());
        }

        [Fact]
        public async Task GetLeads_AsSalesRep_ReturnsOnlyAssignedLeads()
        {
            // Arrange
            string salesRepId = "4";
            SetUserContext(salesRepId, "Sales Representative");

            // Act
            var result = await _controller.GetLeads();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadsProperty = responseValue.GetType().GetProperty("leads");
            var leads = leadsProperty?.GetValue(responseValue) as IEnumerable<Lead>;

            Assert.NotNull(leads);

            var expectedLeads = _context.Leads.Where(l => l.SalesRepAssigned == int.Parse(salesRepId)).ToList();
            Assert.Equal(expectedLeads.Count(), leads.Count());
        }

        [Theory]
        [InlineData(1, "Admin", 74, true)]  // Admin can access any lead
        [InlineData(2, "Manager", 74, true)] // Manager can access assigned leads
        [InlineData(2, "Manager", 95, false)] // Manager can access assigned leads
        [InlineData(4, "Sales Representative", 86, true)] // Sales Rep can access their own leads
        [InlineData(4, "Sales Representative", 96, false)] // Sales Rep can access their own leads
        public async Task GetLeadById_TestAccessControl(int userId, string role, int leadId, bool shouldHaveAccess)
        {
            // Arrange
            SetUserContext(userId.ToString(), role);

            // Act
            var result = await _controller.GetLeadById(leadId);

            // Assert
            if (shouldHaveAccess)
            {
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                Assert.Fail((string?)badRequestResult.Value);
            }
        }

        [Fact]
        public async Task CreateLead_AsAdmin_ReturnsOkResult()
        {
            SetUserContext("1", "Admin");

            var newLead = new Lead
            {
                Name = "Lead Test88",
                Email = "duhfjwn@test.com",
                Phone = "8541236512",
                Source = "Website",
                Status = "New",
                ManagerAssigned = 3,
                SalesRepAssigned = 7
            };

            var result = await _controller.CreateLead(newLead);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadProperty = responseValue.GetType().GetProperty("lead");
            var lead = leadProperty?.GetValue(responseValue) as Lead;

            Assert.NotNull(lead);
            Assert.Equal("Lead Test88", lead.Name);
        }

        [Fact]
        public async Task CreateLead_AsManager_ReturnsOkResult()
        {
            SetUserContext("3", "Manager");

            var newLead = new Lead
            {
                Name = "Lead Test36",
                Email = "skdsjai@test.com",
                Phone = "9652314587",
                Source = "Website",
                Status = "New",
                SalesRepAssigned = 6
            };

            var result = await _controller.CreateLead(newLead);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadProperty = responseValue.GetType().GetProperty("lead");
            var lead = leadProperty?.GetValue(responseValue) as Lead;

            Assert.NotNull(lead);
            Assert.Equal(6, lead.SalesRepAssigned);
        }

        [Fact]
        public async Task CreateLead_AsSalesRep_ReturnsOkResult()
        {
            SetUserContext("5", "Sales Representative");

            var newLead = new Lead
            {
                Name = "Lead Test25",
                Email = "uhajknjah@test.com",
                Phone = "1256941250",
                Source = "Website",
                Status = "New"
            };

            var result = await _controller.CreateLead(newLead);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseValue = okResult.Value;

            var leadProperty = responseValue.GetType().GetProperty("lead");
            var lead = leadProperty?.GetValue(responseValue) as Lead;

            Assert.NotNull(lead);
            Assert.NotNull(lead.ManagerAssigned);
        }

        [Fact]
        public async Task CreateLead_AsSalesRep_ReturnsBadRequest_WhenAssigningManagerOrSalesRep()
        {
            SetUserContext("4", "Sales Representative");

            var newLead = new Lead
            {
                Name = "Lead Test98",
                Email = "hdsoifods@test.com",
                Phone = "6521420786",
                Source = "Website",
                Status = "New",
                ManagerAssigned = 3,
                SalesRepAssigned = 4
            };

            var result = await _controller.CreateLead(newLead);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var responseValue = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value) as string;

            Assert.Fail("Sales Representatives cannot assign Manager or Sales Representative to the lead.");
        }

        [Fact]
        public async Task UpdateLead_AsAdmin_UpdatesLeadSuccessfully()
        {
            // Arrange
            string adminId = "1"; // Assuming 1 is a valid Admin ID in the database
            SetUserContext(adminId, "Admin");

            int leadIdToUpdate = 71; // Assuming 1 is a valid Lead ID in the database
            var existingLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            if (existingLead == null)
            {
                Assert.Fail("Lead not found for the provided ID.");
                return;
            }

            var requestBody = new JsonElement();

            // Construct JSON requestBody
            var json = JsonSerializer.Serialize(new
            {
                Name = "nsckjhais",
                Email = "nsckjhais@example.com",
                Phone = "8475362100",
                Source = "Social Media"
            });

            requestBody = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await _controller.UpdateLead(leadIdToUpdate, requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            Assert.NotNull(updatedLead);
            Assert.Equal("nsckjhais", updatedLead.Name);
            Assert.Equal("nsckjhais@example.com", updatedLead.Email);
            Assert.Equal("8475362100", updatedLead.Phone);
            Assert.Equal("Social Media", updatedLead.Source);
        }


        [Fact]
        public async Task UpdateLead_AsManager_UpdatesLeadSuccessfully()
        {
            // Arrange
            string managerId = "3"; // Assuming 3 is a valid Manager ID in the database
            SetUserContext(managerId, "Manager");

            int leadIdToUpdate = 100; // Assuming 1 is a valid Lead ID in the database
            var existingLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            if (existingLead == null)
            {
                Assert.Fail("Lead not found for the provided ID.");
                return;
            }

            var requestBody = new JsonElement();

            // Construct JSON requestBody
            var json = JsonSerializer.Serialize(new
            {
                Name = "fnejfefe",
                Email = "fnejfefe@example.com",
                Phone = "5412563201",
                Source = "Website"
            });

            requestBody = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await _controller.UpdateLead(leadIdToUpdate, requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            Assert.NotNull(updatedLead);
            Assert.Equal("fnejfefe", updatedLead.Name);
            Assert.Equal("fnejfefe@example.com", updatedLead.Email);
            Assert.Equal("5412563201", updatedLead.Phone);
            Assert.Equal("Website", updatedLead.Source);
        }

        [Fact]
        public async Task UpdateLead_AsSalesRep_UpdatesLeadSuccessfully()
        {
            // Arrange
            string salesRepId = "4"; // Assuming 4 is a valid Sales Representative ID in the database
            SetUserContext(salesRepId, "Sales Representative");

            int leadIdToUpdate = 83; // Assuming 1 is a valid Lead ID in the database and is assigned to this Sales Rep
            var existingLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            if (existingLead == null)
            {
                Assert.Fail("Lead not found for the provided ID.");
                return;
            }

            var requestBody = new JsonElement();

            // Construct JSON requestBody
            var json = JsonSerializer.Serialize(new
            {
                Name = "jskojoiakmd",
                Email = "jskojoiakmd@example.com",
                Phone = "9854712356",
                Source = "Reference"
            });

            requestBody = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await _controller.UpdateLead(leadIdToUpdate, requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            Assert.NotNull(updatedLead);
            Assert.Equal("jskojoiakmd", updatedLead.Name);
            Assert.Equal("jskojoiakmd@example.com", updatedLead.Email);
            Assert.Equal("9854712356", updatedLead.Phone);
            Assert.Equal("Reference", updatedLead.Source);
        }

        [Fact]
        public async Task UpdateLead_UnauthorizedAccess_AsManager_ReturnsUnauthorized()
        {
            // Arrange
            string managerId = "3"; // Assuming 3 is a valid Manager ID in the database
            SetUserContext(managerId, "Manager");

            int leadIdToUpdate = 73; // Assuming 1 is a valid Lead ID in the database, but not assigned to this manager or their team
            var existingLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            if (existingLead == null)
            {
                Assert.Fail("Lead not found for the provided ID.");
                return;
            }

            var requestBody = new JsonElement();

            // Construct JSON requestBody
            var json = JsonSerializer.Serialize(new
            {
                Name = "Unauthorized Update Attempt",
                Email = "unauthorizedmanager@example.com",
                Phone = "0000000000",
                Source = "Invalid Source"
            });

            requestBody = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await _controller.UpdateLead(leadIdToUpdate, requestBody);

            // Assert
            var unauthorizedResult = Assert.IsType<BadRequestObjectResult>(result);
            var responseValue = unauthorizedResult.Value.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value) as string;

            Assert.Equal("You are not authorized to update this lead. You may only update leads assigned to your team.", responseValue);
        }

        [Fact]
        public async Task UpdateLead_UnauthorizedAccess_AsSalesRep_ReturnsUnauthorized()
        {
            // Arrange
            string salesRepId = "4"; // Assuming 4 is a valid Sales Representative ID in the database
            SetUserContext(salesRepId, "Sales Representative");

            int leadIdToUpdate = 89; // Assuming 1 is a valid Lead ID in the database, but not assigned to this Sales Rep
            var existingLead = await _context.Leads.FirstOrDefaultAsync(l => l.Lid == leadIdToUpdate);

            if (existingLead == null)
            {
                Assert.Fail("Lead not found for the provided ID.");
                return;
            }

            var requestBody = new JsonElement();

            // Construct JSON requestBody
            var json = JsonSerializer.Serialize(new
            {
                Name = "Unauthorized Update Attempt",
                Email = "unauthorizedsalesrep@example.com",
                Phone = "0000000000",
                Source = "Invalid Source"
            });

            requestBody = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await _controller.UpdateLead(leadIdToUpdate, requestBody);

            // Assert
            var unauthorizedResult = Assert.IsType<BadRequestObjectResult>(result);
            var responseValue = unauthorizedResult.Value.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value) as string;

            Assert.Equal("You are not authorized to update this lead. This lead is not assigned to you.", responseValue);
        }
    }
}

