//working code(fetch all leads)
using LeadManagment.Controllers;
using LeadManagment.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
                Name = "Lead Test1",
                Email = "njdhfife@test.com",
                Phone = "15415494187",
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
            Assert.Equal("Lead Test1", lead.Name);
        }

        [Fact]
        public async Task CreateLead_AsManager_ReturnsOkResult()
        {
            SetUserContext("3", "Manager");

            var newLead = new Lead
            {
                Name = "Lead Test2",
                Email = "sndjsndsdow@test.com",
                Phone = "781564814687",
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
                Name = "Lead Test3",
                Email = "dmksdjisdwu@test.com",
                Phone = "2541658742",
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
                Name = "Lead Test5",
                Email = "dkfijse@test.com",
                Phone = "9854123658",
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
    }
}

