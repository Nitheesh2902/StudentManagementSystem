using Microsoft.Extensions.Configuration;
using ServiceDesk.Application.Abstractions;

namespace ServiceDesk.Infrastructure.Policies;

/// <summary>Reads ticket rules from appsettings (section "TicketPolicy"), with safe defaults.</summary>
public class ConfigTicketPolicy : ITicketPolicy
{
    private const int DefaultReopenWindowDays = 7;
    private readonly IConfiguration _config;

    public ConfigTicketPolicy(IConfiguration config) => _config = config;

    public int ReopenWindowDays =>
        int.TryParse(_config["TicketPolicy:ReopenWindowDays"], out var days) && days > 0
            ? days
            : DefaultReopenWindowDays;
}
