namespace ServiceDesk.Application.Queues.Dtos;

public sealed record CreateQueueRequest(string Name, string? Description, int? DepartmentId, int? ManagerEmployeeId);

public sealed record UpdateQueueRequest(string Name, string? Description, int? DepartmentId, int? ManagerEmployeeId);

public sealed record AddQueueMemberRequest(int AgentEmployeeId);

public sealed record QueueResponse(
    int Id, string Name, string? Description, int? DepartmentId, int? ManagerEmployeeId, bool IsActive,
    IReadOnlyList<int> MemberEmployeeIds, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
