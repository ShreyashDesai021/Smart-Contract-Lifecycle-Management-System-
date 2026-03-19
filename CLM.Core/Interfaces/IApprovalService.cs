using CLM.Core.Entities;
using CLM.Core.Enums;

namespace CLM.Core.Interfaces;

public interface IApprovalService
{
    Task SubmitForApprovalAsync(int contractId, int userId);
    Task ApproveAsync(int contractId, int approverId, string? comments);
    Task RejectAsync(int contractId, int approverId, string? comments);
    Task<List<Approval>> GetContractApprovalsAsync(int contractId);
    Task<List<Contract>> GetPendingContractsForApproverAsync(int approverId, string role);
}
