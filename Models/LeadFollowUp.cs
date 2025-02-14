using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadFollowUp
{
    public int Fid { get; set; }

    public int? Lid { get; set; }

    public int? Uid { get; set; }

    public DateTime FollowUpDate { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Lead? LidNavigation { get; set; }

    public virtual User? UidNavigation { get; set; }
}
