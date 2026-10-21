namespace ServiceDesk.Application.Abstractions;

/// <summary>Business settings for tickets (read from configuration).</summary>
public interface ITicketPolicy
{
    /// <summary>How many days after resolution the requester may still reopen a ticket.</summary>
    int ReopenWindowDays { get; }
}
