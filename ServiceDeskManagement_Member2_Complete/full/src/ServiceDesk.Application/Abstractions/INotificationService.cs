using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Abstractions;

/// <summary>
/// Owner: Member 3. Called by tickets / assignment on important events.
/// The recipient is an employee id (every user of the system is an employee).
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(
        NotificationType type, int recipientEmployeeId, int ticketId, string? message = null, CancellationToken ct = default);
}
