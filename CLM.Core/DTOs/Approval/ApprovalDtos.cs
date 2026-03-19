namespace CLM.Core.DTOs.Approval;

public class ApprovalActionRequest
{
    public string? Comments { get; set; }
}

public class ApprovalResponse
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string ContractTitle { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public int Level { get; set; }
    public DateTime ActedAt { get; set; }
}
