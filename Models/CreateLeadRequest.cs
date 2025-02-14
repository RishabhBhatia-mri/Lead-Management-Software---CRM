namespace LeadManagment.Models
{
    public class CreateLeadRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }  // Optional, but can be validated if required
        public string Phone { get; set; }
        public string Source { get; set; }  // Enum: 'Website', 'Reference', 'Ads', 'Social Media'
        public string Status { get; set; }  // Enum: 'New', 'Contacted', 'Follow-up', 'Converted', 'Lost'
        public int? ManagerAssigned { get; set; }  // Optional, Manager ID
        public int? SalesRepAssigned { get; set; }  // Optional, Sales Rep ID
    }

}
