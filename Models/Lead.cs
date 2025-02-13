using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class Lead
{
    public int Lid { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string Phone { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? AssignedTo { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<LeadActivityLog> LeadActivityLogs { get; set; } = new List<LeadActivityLog>();

    public virtual ICollection<LeadAssignmentHistory> LeadAssignmentHistories { get; set; } = new List<LeadAssignmentHistory>();

    public virtual ICollection<LeadFollowUp> LeadFollowUps { get; set; } = new List<LeadFollowUp>();

    public virtual ICollection<LeadImportLog> LeadImportLogs { get; set; } = new List<LeadImportLog>();

    public virtual ICollection<LeadStatusHistory> LeadStatusHistories { get; set; } = new List<LeadStatusHistory>();

    public virtual ICollection<LeadUpdateLog> LeadUpdateLogs { get; set; } = new List<LeadUpdateLog>();
}
