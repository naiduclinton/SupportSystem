using SupportTicketing.Core.Enums;

namespace SupportTicketing.Core.Models;

// ── Pagination ────────────────────────────────────────────────────────────
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// ── Ticket search ─────────────────────────────────────────────────────────
public record TicketSearchQuery(
    string? Search = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? AssigneeId = null,
    Guid? TeamId = null,
    Guid? CategoryId = null,
    bool? SlaBreached = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int Page = 1,
    int PageSize = 25,
    string SortBy = "created_at",
    bool SortDesc = true
);

public record TicketSummary(
    Guid Id,
    long TicketNumber,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    string CustomerName,
    string CustomerEmail,
    string? AssigneeName,
    string? TeamName,
    string? CategoryName,
    bool SlaBreached,
    double? SlaCompliancePct,
    double? ResolutionMinutesRemaining,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ── Request models ────────────────────────────────────────────────────────
public record CreateTicketRequest(
    string Subject,
    string? Description,
    TicketPriority Priority,
    TicketChannel Channel,
    string CustomerEmail,
    string? CustomerName,
    Guid? CategoryId,
    Guid? AssigneeId,
    Guid? TeamId,
    List<string>? Tags
);

public record UpdateStatusRequest(
    TicketStatus Status,
    Guid ActorUserId,
    string? Note = null
);

public record AssignTicketRequest(
    Guid? AssigneeId,
    Guid? TeamId,
    Guid ActorUserId
);

public record AddCommentRequest(
    string Body,
    CommentType CommentType,
    Guid? AuthorUserId,
    Guid? AuthorCustomerId
);

// ── Auth models ───────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string FullName,
    string Role
);

// ── Reporting models ──────────────────────────────────────────────────────
public record DashboardStats(
    int OpenCount,
    int InProgressCount,
    int PendingCount,
    int ResolvedTodayCount,
    double AvgFirstResponseHours,
    double AvgResolutionHours,
    double CsatScore,
    int SlaBreachCount
);

public record AgentWorkload(
    Guid UserId,
    string FullName,
    string? TeamName,
    int OpenCount,
    int InProgressCount,
    int PendingCount,
    int SlaBreachCount,
    double? AvgResolutionHours
);

public record SlaReport(
    DateTime From,
    DateTime To,
    int TotalTickets,
    int BreachedCount,
    double CompliancePct,
    IEnumerable<SlaBreachDetail> Breaches
);

public record SlaBreachDetail(
    Guid TicketId,
    long TicketNumber,
    string Subject,
    TicketPriority Priority,
    string? AssigneeName,
    DateTime DueAt,
    DateTime? ResolvedAt
);

// ── RabbitMQ event messages ───────────────────────────────────────────────
public record TicketCreatedEvent(Guid TicketId, long TicketNumber, string Subject, string CustomerEmail, Guid? AssigneeId);
public record TicketAssignedEvent(Guid TicketId, long TicketNumber, Guid AssigneeId, string AssigneeEmail);
public record TicketStatusChangedEvent(Guid TicketId, long TicketNumber, TicketStatus OldStatus, TicketStatus NewStatus);
public record SlaBreachEvent(Guid TicketId, long TicketNumber, string Subject, TicketPriority Priority, string? AssigneeEmail);
public record CsatSurveyEvent(Guid TicketId, Guid CustomerId, string CustomerEmail);
