
using EmployeeServiceDesk.Application.Abstractions;
using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions;

namespace EmployeeServiceDesk.Infrastructure.Persistence;

/// <summary>
/// Uses a SQL Server SEQUENCE to generate unique ticket numbers
/// for concurrent requests.
/// </summary>
public class TicketNumberGenerator : ITicketNumberGenerator
{
    private readonly AppDbContext _db;

    public TicketNumberGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(
        CancellationToken ct = default)
    {
        const string sql =
            "SELECT NEXT VALUE FOR "
            + AppDbContext.TicketNumberSequence
            + " AS [Value]";

        var next = await _db.Database
            .SqlQueryRaw<int>(sql)
            .SingleAsync(ct);

        return $"TKT-{DateTime.UtcNow.Year}-{next:D6}";
    }
}
