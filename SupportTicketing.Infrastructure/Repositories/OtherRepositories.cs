using System.Data;
using Dapper;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Interfaces;

namespace SupportTicketing.Infrastructure.Repositories;

static class EnumHelper
{
    public static string ToSnake(string s)
        => System.Text.RegularExpressions.Regex.Replace(s, "([A-Z])", "_$1").TrimStart('_').ToLower();
}

public class SlaPolicyRepository : ISlaPolicyRepository
{
    private readonly IDbConnection _db;
    public SlaPolicyRepository(IDbConnection db) => _db = db;

    public async Task<SlaPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.QueryFirstOrDefaultAsync<SlaPolicy>("SELECT * FROM sla_policies WHERE id = @Id", new { Id = id });

    public async Task<IEnumerable<SlaPolicy>> GetAllAsync(CancellationToken ct = default)
        => await _db.QueryAsync<SlaPolicy>("SELECT * FROM sla_policies ORDER BY priority");

    public async Task<SlaPolicy?> GetDefaultForPriorityAsync(TicketPriority priority, CancellationToken ct = default)
        => await _db.QueryFirstOrDefaultAsync<SlaPolicy>(
            "SELECT * FROM sla_policies WHERE priority = @Priority::ticket_priority AND is_default = TRUE LIMIT 1",
            new { Priority = priority.ToString().ToLower() });

    public async Task<SlaPolicy> AddAsync(SlaPolicy entity, CancellationToken ct = default)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO sla_policies (id, name, priority, first_response_minutes, resolution_minutes, business_hours_only, is_default, created_at, updated_at)
            VALUES (@Id, @Name, @Priority::ticket_priority, @FirstResponseMinutes, @ResolutionMinutes, @BusinessHoursOnly, @IsDefault, @CreatedAt, @UpdatedAt)",
            new { entity.Id, entity.Name, Priority = entity.Priority.ToString().ToLower(), entity.FirstResponseMinutes, entity.ResolutionMinutes, entity.BusinessHoursOnly, entity.IsDefault, entity.CreatedAt, entity.UpdatedAt });
        return entity;
    }

    public async Task UpdateAsync(SlaPolicy entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.ExecuteAsync(@"
            UPDATE sla_policies SET name=@Name, first_response_minutes=@FirstResponseMinutes,
            resolution_minutes=@ResolutionMinutes, is_default=@IsDefault, updated_at=@UpdatedAt WHERE id=@Id",
            entity);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
        => await _db.ExecuteAsync("DELETE FROM sla_policies WHERE id = @Id", new { Id = id });
}

public class AutomationRuleRepository : IAutomationRuleRepository
{
    private readonly IDbConnection _db;
    public AutomationRuleRepository(IDbConnection db) => _db = db;

    public async Task<AutomationRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.QueryFirstOrDefaultAsync<AutomationRule>("SELECT * FROM automation_rules WHERE id = @Id", new { Id = id });

    public async Task<IEnumerable<AutomationRule>> GetAllAsync(CancellationToken ct = default)
        => await _db.QueryAsync<AutomationRule>("SELECT * FROM automation_rules WHERE is_active = TRUE ORDER BY execution_order");

    public async Task<IEnumerable<AutomationRule>> GetByTriggerAsync(AutomationTrigger trigger, CancellationToken ct = default)
        => await _db.QueryAsync<AutomationRule>(
            "SELECT * FROM automation_rules WHERE trigger_event = @Trigger::automation_trigger AND is_active = TRUE ORDER BY execution_order",
            new { Trigger = trigger.ToString().ToLower() });

    public async Task<AutomationRule> AddAsync(AutomationRule entity, CancellationToken ct = default)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO automation_rules (id, name, is_active, trigger_event, conditions, actions, execution_order, created_at, updated_at)
            VALUES (@Id, @Name, @IsActive, @TriggerEvent::automation_trigger, @Conditions::jsonb, @Actions::jsonb, @ExecutionOrder, @CreatedAt, @UpdatedAt)",
            new { entity.Id, entity.Name, entity.IsActive, TriggerEvent = entity.TriggerEvent.ToString().ToLower(), Conditions = "[]", Actions = "[]", entity.ExecutionOrder, entity.CreatedAt, entity.UpdatedAt });
        return entity;
    }

    public async Task UpdateAsync(AutomationRule entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.ExecuteAsync("UPDATE automation_rules SET name=@Name, is_active=@IsActive, updated_at=@UpdatedAt WHERE id=@Id", entity);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
        => await _db.ExecuteAsync("UPDATE automation_rules SET is_active = FALSE WHERE id = @Id", new { Id = id });
}

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnection _db;
    public NotificationRepository(IDbConnection db) => _db = db;

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.QueryFirstOrDefaultAsync<Notification>("SELECT * FROM notifications WHERE id = @Id", new { Id = id });

    public async Task<IEnumerable<Notification>> GetAllAsync(CancellationToken ct = default)
        => await _db.QueryAsync<Notification>("SELECT * FROM notifications ORDER BY created_at DESC");

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.QueryAsync<Notification>(
            "SELECT * FROM notifications WHERE user_id = @UserId AND is_read = FALSE ORDER BY created_at DESC",
            new { UserId = userId });

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => await _db.ExecuteAsync(
            "UPDATE notifications SET is_read = TRUE, read_at = NOW() WHERE user_id = @UserId AND is_read = FALSE",
            new { UserId = userId });

    public async Task<Notification> AddAsync(Notification entity, CancellationToken ct = default)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO notifications (id, user_id, ticket_id, channel, subject, body, is_read, created_at)
            VALUES (@Id, @UserId, @TicketId, @Channel::notification_channel, @Subject, @Body, @IsRead, @CreatedAt)",
            new { entity.Id, entity.UserId, entity.TicketId, Channel = entity.Channel.ToString().ToLower(), entity.Subject, entity.Body, entity.IsRead, entity.CreatedAt });
        return entity;
    }

    public async Task UpdateAsync(Notification entity, CancellationToken ct = default)
        => await _db.ExecuteAsync("UPDATE notifications SET is_read=@IsRead, read_at=@ReadAt WHERE id=@Id", entity);

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
        => await _db.ExecuteAsync("DELETE FROM notifications WHERE id = @Id", new { Id = id });
}

public class CommentRepository : ICommentRepository
{
    private readonly IDbConnection _db;
    public CommentRepository(IDbConnection db) => _db = db;

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.QueryFirstOrDefaultAsync<Comment>("SELECT * FROM comments WHERE id = @Id AND deleted_at IS NULL", new { Id = id });

    public async Task<IEnumerable<Comment>> GetAllAsync(CancellationToken ct = default)
        => await _db.QueryAsync<Comment>("SELECT * FROM comments WHERE deleted_at IS NULL");

    public async Task<IEnumerable<Comment>> GetByTicketAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.QueryAsync<Comment>(
            "SELECT * FROM comments WHERE ticket_id = @TicketId AND deleted_at IS NULL ORDER BY created_at",
            new { TicketId = ticketId });

    public async Task<Comment> AddAsync(Comment entity, CancellationToken ct = default)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO comments (id, ticket_id, author_user_id, author_customer_id, comment_type, body, is_edited, created_at, updated_at)
            VALUES (@Id, @TicketId, @AuthorUserId, @AuthorCustomerId, @CommentType::comment_type, @Body, @IsEdited, @CreatedAt, @UpdatedAt)",
            new { entity.Id, entity.TicketId, entity.AuthorUserId, entity.AuthorCustomerId, CommentType = entity.CommentType.ToString().ToLower(), entity.Body, entity.IsEdited, entity.CreatedAt, entity.UpdatedAt });
        return entity;
    }

    public async Task UpdateAsync(Comment entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.ExecuteAsync("UPDATE comments SET body=@Body, is_edited=TRUE, updated_at=@UpdatedAt WHERE id=@Id", entity);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
        => await _db.ExecuteAsync("UPDATE comments SET deleted_at = NOW() WHERE id = @Id", new { Id = id });
}
