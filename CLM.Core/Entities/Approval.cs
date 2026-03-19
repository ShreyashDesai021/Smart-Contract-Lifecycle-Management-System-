using CLM.Core.Enums;

namespace CLM.Core.Entities;

public class Approval
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int ApproverId { get; set; }
    public ApprovalAction Action { get; set; }
    public string? Comments { get; set; }
    public int Level { get; set; } // 1 = Manager, 2 = Admin
    public DateTime ActedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
    public User Approver { get; set; } = null!;
}
