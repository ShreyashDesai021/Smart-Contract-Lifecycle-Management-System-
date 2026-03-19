using System.Security.Claims;
using CLM.Core.DTOs.Approval;
using CLM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IAuditService _audit;

    public ApprovalsController(IApprovalService approvalService, IAuditService audit)
    {
        _approvalService = approvalService;
        _audit = audit;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";

    /// <summary>Submit a Draft contract for approval (User)</summary>
    [HttpPost("contracts/{contractId}/submit")]
    [Authorize(Roles = "User,Manager,Admin")]
    public async Task<IActionResult> Submit(int contractId)
    {
        await _approvalService.SubmitForApprovalAsync(contractId, GetUserId());
        return Ok(new { message = "Contract submitted for approval" });
    }

    /// <summary>Approve a contract (Manager = Level 1, Admin = Level 2/Final)</summary>
    [HttpPost("contracts/{contractId}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Approve(int contractId, [FromBody] ApprovalActionRequest request)
    {
        await _approvalService.ApproveAsync(contractId, GetUserId(), request.Comments);
        return Ok(new { message = "Contract approved" });
    }

    /// <summary>Reject a contract (Manager or Admin)</summary>
    [HttpPost("contracts/{contractId}/reject")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Reject(int contractId, [FromBody] ApprovalActionRequest request)
    {
        await _approvalService.RejectAsync(contractId, GetUserId(), request.Comments);
        return Ok(new { message = "Contract rejected" });
    }

    /// <summary>Get approval history for a contract</summary>
    [HttpGet("contracts/{contractId}")]
    public async Task<IActionResult> GetContractApprovals(int contractId)
    {
        var approvals = await _approvalService.GetContractApprovalsAsync(contractId);
        var response = approvals.Select(a => new ApprovalResponse
        {
            Id = a.Id,
            ContractId = a.ContractId,
            ContractTitle = a.Contract?.Title ?? "",
            ApproverName = a.Approver?.Name ?? "",
            Action = a.Action.ToString(),
            Comments = a.Comments,
            Level = a.Level,
            ActedAt = a.ActedAt
        });
        return Ok(response);
    }

    /// <summary>Get contracts pending approval for the current user's role</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetPendingForMe()
    {
        var contracts = await _approvalService.GetPendingContractsForApproverAsync(GetUserId(), GetUserRole());
        return Ok(contracts.Select(c => new
        {
            c.Id,
            c.Title,
            c.Parties,
            c.Value,
            c.StartDate,
            c.EndDate,
            Status = c.Status.ToString(),
            c.CreatedAt
        }));
    }
}
