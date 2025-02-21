using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace LeadManagment.Models {
    public class LeadCsvModel {
        [Name("Name")]
        [Required]
        public string Name { get; set; } = null!;

        [Name("Email")]
        public string? Email { get; set; }

        [Name("Phone")]
        [Required]
        public string Phone { get; set; } = null!;

        [Name("Source")]
        public string? Source { get; set; }

        [Name("Status")]
        public string? Status { get; set; }

        [Name("SalesRepAssigned")]
        public int? SalesRepAssigned { get; set; }
    }
}
