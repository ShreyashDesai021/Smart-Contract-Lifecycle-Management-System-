using CLM.Core.Entities;
using CLM.Core.Enums;
using CLM.Core.Interfaces;
using CLM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CLM.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;

    public ApprovalService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task SubmitForApprovalAsync(int contractId, int userId)
    {
        var contract = await _ctx.Contracts.FindAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found");

        if (contract.Status != ContractStatus.Draft)
            throw new InvalidOperationException("Only Draft contracts can be submitted for approval");

        contract.Status = ContractStatus.Pending;
        contract.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        await _audit.LogAsync(userId, "Submit", "Contract", contractId, $"Contract '{contract.Title}' submitted for approval");
    }

    public async Task ApproveAsync(int contractId, int approverId, string? comments)
    {
        var contract = await _ctx.Contracts.FindAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found");

        if (contract.Status != ContractStatus.Pending)
            throw new InvalidOperationException("Only Pending contracts can be approved");

        var approver = await _ctx.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == approverId)
            ?? throw new KeyNotFoundException("Approver not found");

        var level = approver.Role.Name == "Manager" ? 1 : 2;

        // Check if Manager already approved (for Admin final approval)
        if (approver.Role.Name == "Admin")
        {
            var managerApproved = await _ctx.Approvals.AnyAsync(a =>
                a.ContractId == contractId && a.Level == 1 && a.Action == ApprovalAction.Approve);
            if (!managerApproved)
                throw new InvalidOperationException("A Manager must approve before Admin can approve");

            contract.Status = ContractStatus.Approved;
        }

        var approval = new Approval
        {
            ContractId = contractId,
            ApproverId = approverId,
            Action = ApprovalAction.Approve,
            Comments = comments,
            Level = level,
            ActedAt = DateTime.UtcNow
        };

        _ctx.Approvals.Add(approval);
        contract.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        await _audit.LogAsync(approverId, "Approve", "Contract", contractId,
            $"Contract '{contract.Title}' approved at level {level}");
    }

    public async Task RejectAsync(int contractId, int approverId, string? comments)
    {
        var contract = await _ctx.Contracts.FindAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found");

        if (contract.Status != ContractStatus.Pending)
            throw new InvalidOperationException("Only Pending contracts can be rejected");

        var approver = await _ctx.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == approverId)
            ?? throw new KeyNotFoundException("Approver not found");

        var approval = new Approval
        {
            ContractId = contractId,
            ApproverId = approverId,
            Action = ApprovalAction.Reject,
            Comments = comments,
            Level = approver.Role.Name == "Manager" ? 1 : 2,
            ActedAt = DateTime.UtcNow
        };

        _ctx.Approvals.Add(approval);
        contract.Status = ContractStatus.Rejected;
        contract.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        await _audit.LogAsync(approverId, "Reject", "Contract", contractId,
            $"Contract '{contract.Title}' rejected");
    }

    public async Task<List<Approval>> GetContractApprovalsAsync(int contractId) =>
        await _ctx.Approvals
            .Include(a => a.Approver).ThenInclude(u => u.Role)
            .Where(a => a.ContractId == contractId)
            .OrderBy(a => a.ActedAt)
            .ToListAsync();

    public async Task<List<Contract>> GetPendingContractsForApproverAsync(int approverId, string role)
    {
        var level = role == "Manager" ? 1 : 2;

        // Get contract IDs already acted on by this approver
        var actedIds = await _ctx.Approvals
            .Where(a => a.ApproverId == approverId)
            .Select(a => a.ContractId)
            .ToListAsync();

        var query = _ctx.Contracts
            .Include(c => c.CreatedBy)
            .Where(c => c.Status == ContractStatus.Pending && !actedIds.Contains(c.Id));

        // For Admin: only show contracts that a Manager has already approved
        if (role == "Admin")
        {
            var managerApprovedIds = await _ctx.Approvals
                .Where(a => a.Level == 1 && a.Action == ApprovalAction.Approve)
                .Select(a => a.ContractId)
                .ToListAsync();
            query = query.Where(c => managerApprovedIds.Contains(c.Id));
        }

        return await query.ToListAsync();
    }
}
