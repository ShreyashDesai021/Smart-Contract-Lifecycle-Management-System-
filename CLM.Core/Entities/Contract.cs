using CLM.Core.Enums;

namespace CLM.Core.Entities;

public class Contract
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Parties { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Value { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public string? FilePath { get; set; }
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<ContractVersion> Versions { get; set; } = new List<ContractVersion>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
