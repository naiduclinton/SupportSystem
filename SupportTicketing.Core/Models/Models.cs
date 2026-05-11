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

public class TicketSummary
{
    public Guid Id { get; set; }
    public long TicketNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string? TeamName { get; set; }
    public string? CategoryName { get; set; }
    public bool SlaBreached { get; set; }
    public double? SlaCompliancePct { get; set; }
    public double? ResolutionMinutesRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Request models ────────────────────────────────────────────────────────
public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TicketChannel Channel { get; set; } = TicketChannel.Portal;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? TeamId { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateStatusRequest
{
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TicketStatus Status { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Note { get; set; }
}

public class AssignTicketRequest
{
    public Guid? AssigneeId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid ActorUserId { get; set; }
}

public class AddCommentRequest
{
    public string Body { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public CommentType CommentType { get; set; } = CommentType.Reply;
    public Guid? AuthorUserId { get; set; }
    public Guid? AuthorCustomerId { get; set; }
}

// ── Auth models ───────────────────────────────────────────────────────────
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

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
