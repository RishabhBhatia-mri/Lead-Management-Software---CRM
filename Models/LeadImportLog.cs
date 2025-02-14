using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class LeadImportLog
{
    public int ImportId { get; set; }

    public int? LogId { get; set; }

    public int? Lid { get; set; }

    public int? ImportedBy { get; set; }

    public DateTime? ImportedAt { get; set; }

    public virtual User? ImportedByNavigation { get; set; }

    public virtual Lead? LidNavigation { get; set; }

    public virtual ImportExportLog? Log { get; set; }
}
