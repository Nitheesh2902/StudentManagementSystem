using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Common;

public static class CurrentUserExtensions
{
    /// <summary>Agent, manager or administrator.</summary>
    public static bool IsStaff(this ICurrentUserService user)
        => user.IsInRole(Roles.Agent) || user.IsInRole(Roles.Manager) || user.IsInRole(Roles.Administrator);

    public static bool IsManagerOrAdmin(this ICurrentUserService user)
        => user.IsInRole(Roles.Manager) || user.IsInRole(Roles.Administrator);

    public static int RequireEmployeeId(this ICurrentUserService user)
        => user.EmployeeId ?? throw new UnauthorizedAccessException("You must be signed in as an employee.");

    public static void RequireAuthenticated(this ICurrentUserService user)
    {
        if (!user.IsAuthenticated)
            throw new UnauthorizedAccessException("You must be signed in.");
    }

    public static void RequireStaff(this ICurrentUserService user)
    {
        user.RequireAuthenticated();
        if (!user.IsStaff())
            throw new ForbiddenException("Only service desk staff can do this.");
    }

    public static void RequireManagerOrAdmin(this ICurrentUserService user)
    {
        user.RequireAuthenticated();
        if (!user.IsManagerOrAdmin())
            throw new ForbiddenException("Only service desk managers and administrators can do this.");
    }

    public static void RequireAdmin(this ICurrentUserService user)
    {
        user.RequireAuthenticated();
        if (!user.IsInRole(Roles.Administrator))
            throw new ForbiddenException("Only administrators can do this.");
    }
}
