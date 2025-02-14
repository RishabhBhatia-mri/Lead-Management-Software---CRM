namespace LeadManagment.Dashboards
{
    public class ManagerDashboard
    {
        public object GetContent()
        {
            return new
            {
                dashboardType = "Manager",
                permissions = new string[] { "Assign Leads", "View Team Performance", "Manage Tasks" },
                customMessage = "Manager Dashboard loaded successfully"
            };
        }
    }
}
