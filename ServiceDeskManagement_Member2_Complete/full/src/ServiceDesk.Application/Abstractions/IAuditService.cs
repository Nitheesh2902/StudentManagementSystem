namespace ServiceDesk.Application.Abstractions;

/// <summary>Owner: Member 4. The implementation takes the actor from ICurrentUserService.</summary>
public interface IAuditService
{
    Task LogAsync(string action, string entityName, string entityId, string? details = null, CancellationToken ct = default);
}
