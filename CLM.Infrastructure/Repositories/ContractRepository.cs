using CLM.Core.DTOs.Contract;
using CLM.Core.Entities;
using CLM.Core.Enums;
using CLM.Core.Interfaces;
using CLM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CLM.Infrastructure.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly ApplicationDbContext _ctx;
    public ContractRepository(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<Contract?> GetByIdAsync(int id) =>
        await _ctx.Contracts
            .Include(c => c.CreatedBy).ThenInclude(u => u.Role)
            .Include(c => c.Versions)
            .Include(c => c.Approvals).ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<PagedResult<Contract>> GetAllAsync(ContractFilterRequest filter)
    {
        var query = _ctx.Contracts
            .Include(c => c.CreatedBy)
            .Include(c => c.Versions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(c => c.Title.Contains(filter.Search) || c.Parties.Contains(filter.Search));

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<ContractStatus>(filter.Status, true, out var status))
            query = query.Where(c => c.Status == status);

        if (filter.StartDateFrom.HasValue)
            query = query.Where(c => c.StartDate >= filter.StartDateFrom.Value);

        if (filter.StartDateTo.HasValue)
            query = query.Where(c => c.StartDate <= filter.StartDateTo.Value);

        if (filter.MinValue.HasValue)
            query = query.Where(c => c.Value >= filter.MinValue.Value);

        if (filter.MaxValue.HasValue)
            query = query.Where(c => c.Value <= filter.MaxValue.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Contract>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Contract> CreateAsync(Contract contract)
    {
        _ctx.Contracts.Add(contract);
        await _ctx.SaveChangesAsync();
        return contract;
    }

    public async Task<Contract> UpdateAsync(Contract contract)
    {
        contract.UpdatedAt = DateTime.UtcNow;
        _ctx.Contracts.Update(contract);
        await _ctx.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contract = await _ctx.Contracts.FindAsync(id);
        if (contract == null) return false;
        _ctx.Contracts.Remove(contract);
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<List<Contract>> GetExpiringContractsAsync(int days) =>
        await _ctx.Contracts
            .Include(c => c.CreatedBy)
            .Where(c => c.Status == ContractStatus.Approved &&
                        c.EndDate >= DateTime.UtcNow &&
                        c.EndDate <= DateTime.UtcNow.AddDays(days))
            .ToListAsync();

    public async Task<List<Contract>> GetByStatusAsync(ContractStatus status) =>
        await _ctx.Contracts.Where(c => c.Status == status).ToListAsync();

    public async Task<int> GetNextVersionNumberAsync(int contractId)
    {
        var max = await _ctx.ContractVersions
            .Where(v => v.ContractId == contractId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        return max + 1;
    }

    public async Task SaveVersionAsync(ContractVersion version)
    {
        _ctx.ContractVersions.Add(version);
        await _ctx.SaveChangesAsync();
    }
}
