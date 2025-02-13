using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadUpdateLog
{
    public int LogId { get; set; }

    public int? Lid { get; set; }

    public int? Uid { get; set; }

    public string FieldUpdated { get; set; } = null!;

    public string? OldValue { get; set; }

    public string NewValue { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public virtual Lead? LidNavigation { get; set; }

    public virtual User? UidNavigation { get; set; }
}
