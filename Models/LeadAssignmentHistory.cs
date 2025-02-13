using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadAssignmentHistory
{
    public int Aid { get; set; }

    public int? Lid { get; set; }

    public int? AssignedTo { get; set; }

    public int? AssignedBy { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual User? AssignedByNavigation { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual Lead? LidNavigation { get; set; }
}
