using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Core.Interfaces;

// ── Generic repository ────────────────────────────────────────────────────
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}

// ── Ticket-specific query interface ───────────────────────────────────────
public interface ITicketRepository : IRepository<Ticket>
{
    Task<PagedResult<TicketSummary>> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);
    Task<Ticket?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Ticket>> GetOverdueSlaTicketsAsync(CancellationToken ct = default);
    Task<IEnumerable<Ticket>> GetByAssigneeAsync(Guid assigneeId, CancellationToken ct = default);
    Task AssignTagsAsync(Guid ticketId, IEnumerable<Guid> tagIds, CancellationToken ct = default);
}

// ── User repository ────────────────────────────────────────────────────────
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<AgentWorkload>> GetAgentWorkloadsAsync(CancellationToken ct = default);
}

// ── Customer repository ────────────────────────────────────────────────────
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Customer> GetOrCreateAsync(string email, string? fullName, CancellationToken ct = default);
}

// ── Comment repository ─────────────────────────────────────────────────────
public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByTicketAsync(Guid ticketId, CancellationToken ct = default);
}

// ── SLA policy repository ──────────────────────────────────────────────────
public interface ISlaPolicyRepository : IRepository<SlaPolicy>
{
    Task<SlaPolicy?> GetDefaultForPriorityAsync(TicketPriority priority, CancellationToken ct = default);
}

// ── Notification repository ────────────────────────────────────────────────
public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

// ── Automation rule repository ─────────────────────────────────────────────
public interface IAutomationRuleRepository : IRepository<AutomationRule>
{
    Task<IEnumerable<AutomationRule>> GetByTriggerAsync(AutomationTrigger trigger, CancellationToken ct = default);
}

// ── CSAT repository ────────────────────────────────────────────────────────
public interface ICsatRepository : IRepository<CsatSurvey>
{
    Task<double> GetAverageScoreAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
