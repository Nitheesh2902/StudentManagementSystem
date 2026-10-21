using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Stubs;

// Temporary implementations so every member can build and run alone.
// Each owner replaces their stub with the real implementation and changes the DI line.

/// <summary>
/// Member 1 replaces this with a JWT-based implementation.
/// Until then the "signed-in user" comes from the DevUser section of appsettings.json.
/// </summary>
public class StubCurrentUserService : ICurrentUserService
{
    private readonly IConfiguration _config;
    public StubCurrentUserService(IConfiguration config) => _config = config;

    public int? UserId => ReadInt("DevUser:UserId");
    public int? EmployeeId => ReadInt("DevUser:EmployeeId");
    public string? Role => _config["DevUser:Role"];
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    private int? ReadInt(string key) => int.TryParse(_config[key], out var value) ? value : null;
}

public class StubSlaService : ISlaService
{
    public Task<SlaDeadlines> CalculateDeadlinesAsync(
        TicketPriority priority, int serviceItemId, DateTime createdAtUtc, CancellationToken ct = default)
        => Task.FromResult(new SlaDeadlines(null, null));
}

public class StubNotificationService : INotificationService
{
    private readonly ILogger<StubNotificationService> _logger;
    public StubNotificationService(ILogger<StubNotificationService> logger) => _logger = logger;

    public Task NotifyAsync(
        NotificationType type, int recipientEmployeeId, int ticketId, string? message = null, CancellationToken ct = default)
    {
        _logger.LogInformation("NOTIFY (stub) {Type} -> employee {EmployeeId}, ticket {TicketId}: {Message}",
            type, recipientEmployeeId, ticketId, message);
        return Task.CompletedTask;
    }
}

public class StubAuditService : IAuditService
{
    private readonly ILogger<StubAuditService> _logger;
    public StubAuditService(ILogger<StubAuditService> logger) => _logger = logger;

    public Task LogAsync(string action, string entityName, string entityId, string? details = null, CancellationToken ct = default)
    {
        _logger.LogInformation("AUDIT (stub) {Action} {Entity}#{EntityId} {Details}", action, entityName, entityId, details);
        return Task.CompletedTask;
    }
}

public class StubAssetLinkService : IAssetLinkService
{
    public Task<bool> AssetExistsAsync(int assetId, CancellationToken ct = default) => Task.FromResult(true);
    public Task LinkTicketAsync(int assetId, int ticketId, CancellationToken ct = default) => Task.CompletedTask;
}
