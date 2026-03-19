namespace CLM.Core.Entities;

public class ContractVersion
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int VersionNumber { get; set; }
    public string Snapshot { get; set; } = string.Empty; // JSON snapshot of contract fields
    public string ChangeNotes { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
