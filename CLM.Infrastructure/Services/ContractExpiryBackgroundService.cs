using CLM.Core.Enums;
using CLM.Core.Interfaces;
using CLM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CLM.Infrastructure.Services;

public class ContractExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContractExpiryBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public ContractExpiryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<ContractExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Contract Expiry Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiringContractsAsync();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckExpiringContractsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var today = DateTime.UtcNow;
            var soon = today.AddDays(30);

            var expiring = await ctx.Contracts
                .Include(c => c.CreatedBy)
                .Where(c => c.Status == ContractStatus.Approved &&
                            c.EndDate >= today && c.EndDate <= soon)
                .ToListAsync();

            // Mark fully expired contracts
            var expired = await ctx.Contracts
                .Where(c => c.Status == ContractStatus.Approved && c.EndDate < today)
                .ToListAsync();

            foreach (var c in expired)
                c.Status = ContractStatus.Expired;

            if (expired.Any())
            {
                await ctx.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} contracts as Expired", expired.Count);
            }

            foreach (var contract in expiring)
            {
                var daysLeft = (int)(contract.EndDate - today).TotalDays;
                await emailService.SendExpiryReminderAsync(
                    contract.CreatedBy.Email, contract.Title, daysLeft);
            }

            _logger.LogInformation("Expiry check complete. {Count} alerts sent.", expiring.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in expiry check");
        }
    }
}
