using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

/// <summary>Immutable record of one change to a ticket.</summary>
public class TicketHistory
{
    private const int MaxValueLength = 500;

    private TicketHistory() { } // EF Core

    public TicketHistory(
        TicketHistoryAction action, string? fieldName, string? oldValue, string? newValue,
        int? changedByUserId, string? note = null)
    {
        Action = action;
        FieldName = fieldName;
        OldValue = Truncate(oldValue);
        NewValue = Truncate(newValue);
        Note = Truncate(note);
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public TicketHistoryAction Action { get; private set; }
    public string? FieldName { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Note { get; private set; }
    public int? ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private static string? Truncate(string? value)
        => value is { Length: > MaxValueLength } ? value[..MaxValueLength] : value;
}
