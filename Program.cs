using LeadManagment.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure database context
builder.Services.AddDbContext<LeadsManagementContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("lmdb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("lmdb"))));


var app = builder.Build();

// Middleware
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Set default route to /auth/login
app.UseEndpoints(endpoints => {
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "auth/login",
        defaults: new { controller = "Auth", action = "Login" }
    );
});

app.Run();