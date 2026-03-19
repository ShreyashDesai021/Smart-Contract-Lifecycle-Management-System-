namespace CLM.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string entityType, int? entityId = null, string? details = null);
}
