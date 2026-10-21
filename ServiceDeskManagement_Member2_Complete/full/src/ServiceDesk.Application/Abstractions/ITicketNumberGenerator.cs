namespace ServiceDesk.Application.Abstractions;

public interface ITicketNumberGenerator
{
    /// <summary>Returns the next unique number, e.g. TKT-2026-000123.</summary>
    Task<string> NextAsync(CancellationToken ct = default);
}
