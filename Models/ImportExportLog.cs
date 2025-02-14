using System;
using System.Collections.Generic;

namespace LeadManagment.Models;

public partial class ImportExportLog
{
    public int LogId { get; set; }

    public int? Uid { get; set; }

    public string? ActionType { get; set; }

    public string FileName { get; set; } = null!;

    public DateTime? LogTimestamp { get; set; }

    public virtual ICollection<LeadImportLog> LeadImportLogs { get; set; } = new List<LeadImportLog>();

    public virtual User? UidNavigation { get; set; }
}
