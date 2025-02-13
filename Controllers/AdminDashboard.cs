namespace LeadManagment.Dashboards {
    public class AdminDashboard {
        public object GetContent() {
            return new {
                dashboardType = "Admin",
                permissions = new string[] { "View All Leads", "Manage Users", "Generate Reports" },
                customMessage = "Admin Dashboard loaded successfully"
            };
        }
    }
}
