using CLM.Core.DTOs.Analytics;
using CLM.Core.Enums;
using CLM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _ctx;

    public AnalyticsController(ApplicationDbContext ctx) => _ctx = ctx;

    /// <summary>Get full dashboard summary with charts data</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.UtcNow;
        var contracts = await _ctx.Contracts.Include(c => c.CreatedBy).ToListAsync();

        var summary = new DashboardSummary
        {
            TotalContracts = contracts.Count,
            DraftContracts = contracts.Count(c => c.Status == ContractStatus.Draft),
            PendingContracts = contracts.Count(c => c.Status == ContractStatus.Pending),
            ApprovedContracts = contracts.Count(c => c.Status == ContractStatus.Approved),
            RejectedContracts = contracts.Count(c => c.Status == ContractStatus.Rejected),
            ExpiringIn30Days = contracts.Count(c =>
                c.Status == ContractStatus.Approved &&
                c.EndDate >= today && c.EndDate <= today.AddDays(30)),
            ExpiringIn7Days = contracts.Count(c =>
                c.Status == ContractStatus.Approved &&
                c.EndDate >= today && c.EndDate <= today.AddDays(7)),
            TotalContractValue = contracts.Where(c => c.Status == ContractStatus.Approved).Sum(c => c.Value),

            StatusDistribution = Enum.GetValues<ContractStatus>()
                .Select(s => new StatusDistribution
                {
                    Status = s.ToString(),
                    Count = contracts.Count(c => c.Status == s)
                }).ToList(),

            MonthlyTrends = contracts
                .Where(c => c.CreatedAt >= today.AddMonths(-12))
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyTrend
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count(),
                    Value = g.Sum(c => c.Value)
                }).ToList(),

            ExpiringContracts = contracts
                .Where(c => c.Status == ContractStatus.Approved && c.EndDate >= today && c.EndDate <= today.AddDays(30))
                .OrderBy(c => c.EndDate)
                .Select(c => new ExpiringContractInfo
                {
                    Id = c.Id,
                    Title = c.Title,
                    Parties = c.Parties,
                    EndDate = c.EndDate,
                    DaysUntilExpiry = (int)(c.EndDate - today).TotalDays
                }).ToList()
        };

        return Ok(summary);
    }
}
