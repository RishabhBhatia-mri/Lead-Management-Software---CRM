using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadActivityLog
{
    public int Aid { get; set; }

    public int? Lid { get; set; }

    public int? Uid { get; set; }

    public DateTime ActivityDate { get; set; }

    public string? Notes { get; set; }

    public bool Responded { get; set; }

    public virtual Lead? LidNavigation { get; set; }

    public virtual User? UidNavigation { get; set; }
}
