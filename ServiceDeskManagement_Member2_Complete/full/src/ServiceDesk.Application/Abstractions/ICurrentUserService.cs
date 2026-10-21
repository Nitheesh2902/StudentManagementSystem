namespace ServiceDesk.Application.Abstractions;

/// <summary>Owner: Member 1. Reads the identity from the JWT.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    int? EmployeeId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
