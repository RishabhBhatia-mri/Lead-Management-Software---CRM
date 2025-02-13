namespace LeadManagment.Dashboards {
    public class SalesDashboard {
        public object GetContent() {
            return new {
                dashboardType = "Sales Representative",
                permissions = new string[] { "View Assigned Leads", "Update Lead Status", "Schedule Follow-ups" },
                customMessage = "Sales Representative Dashboard loaded successfully"
            };
        }
    }
}
