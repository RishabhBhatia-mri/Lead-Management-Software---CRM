using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class User
{
    public int Uid { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string PhoneNo { get; set; } = null!;

    public DateTime DateOfJoining { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? ReportsTo { get; set; }

    public virtual ICollection<ImportExportLog> ImportExportLogs { get; set; } = new List<ImportExportLog>();

    public virtual ICollection<User> InverseReportsToNavigation { get; set; } = new List<User>();

    public virtual ICollection<LeadActivityLog> LeadActivityLogs { get; set; } = new List<LeadActivityLog>();

    public virtual ICollection<Lead> LeadAssignedToNavigations { get; set; } = new List<Lead>();

    public virtual ICollection<LeadAssignmentHistory> LeadAssignmentHistoryAssignedByNavigations { get; set; } = new List<LeadAssignmentHistory>();

    public virtual ICollection<LeadAssignmentHistory> LeadAssignmentHistoryAssignedToNavigations { get; set; } = new List<LeadAssignmentHistory>();

    public virtual ICollection<Lead> LeadCreatedByNavigations { get; set; } = new List<Lead>();

    public virtual ICollection<LeadFollowUp> LeadFollowUps { get; set; } = new List<LeadFollowUp>();

    public virtual ICollection<LeadImportLog> LeadImportLogs { get; set; } = new List<LeadImportLog>();

    public virtual ICollection<LeadStatusHistory> LeadStatusHistories { get; set; } = new List<LeadStatusHistory>();

    public virtual ICollection<LeadUpdateLog> LeadUpdateLogs { get; set; } = new List<LeadUpdateLog>();

    public virtual User? ReportsToNavigation { get; set; }
}
