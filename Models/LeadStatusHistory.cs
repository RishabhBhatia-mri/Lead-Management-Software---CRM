using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadStatusHistory
{
    public int Sid { get; set; }

    public int? Lid { get; set; }

    public int? Uid { get; set; }

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public DateTime? TimeOfChange { get; set; }

    public virtual Lead? LidNavigation { get; set; }

    public virtual User? UidNavigation { get; set; }
}
