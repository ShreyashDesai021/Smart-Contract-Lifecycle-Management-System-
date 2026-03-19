using CLM.Core.Entities;
using CLM.Core.Interfaces;
using CLM.Infrastructure.Data;

namespace CLM.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _ctx;
    public AuditService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task LogAsync(int userId, string action, string entityType, int? entityId = null, string? details = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };
        _ctx.AuditLogs.Add(log);
        await _ctx.SaveChangesAsync();
    }
}
