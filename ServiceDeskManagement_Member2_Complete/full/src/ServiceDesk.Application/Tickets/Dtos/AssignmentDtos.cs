namespace ServiceDesk.Application.Tickets.Dtos;

public sealed record AssignTicketRequest(int AgentEmployeeId, string? Note);

public sealed record ReassignTicketRequest(int AgentEmployeeId, string Reason);

/// <summary>Give exactly one of TargetQueueId (another team) or ManagerEmployeeId (a manager).</summary>
public sealed record EscalateTicketRequest(int? TargetQueueId, int? ManagerEmployeeId, string Reason);

public sealed record AgentWorkloadResponse(
    int AgentEmployeeId, int Assigned, int InProgress, int WaitingForUser, int Reopened,
    int ActiveTotal, int HighPriority);
