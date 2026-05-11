using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly IDbConnection _db;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(IDbConnection db, ILogger<TicketRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                t.id, t.ticket_number, t.subject, t.description,
                t.status::text AS status, t.priority::text AS priority,
                t.channel::text AS channel, t.customer_id, t.assignee_id,
                t.team_id, t.category_id, t.sla_policy_id,
                t.first_response_due_at, t.resolution_due_at,
                t.first_responded_at, t.resolved_at, t.closed_at,
                t.sla_breached, t.external_ref, t.created_at, t.updated_at,
                c.id, c.email, c.full_name, c.phone, c.company,
                u.id, u.email, u.full_name, u.role::text AS role, u.is_active,
                tm.id, tm.name
            FROM tickets t
            JOIN customers c   ON c.id = t.customer_id
            LEFT JOIN users u  ON u.id = t.assignee_id
            LEFT JOIN teams tm ON tm.id = t.team_id
            WHERE t.id = @Id AND t.deleted_at IS NULL";

        var result = await _db.QueryAsync<Ticket, Customer, User, Team, Ticket>(
            sql,
            (ticket, customer, user, team) =>
            {
                ticket.Customer = customer;
                ticket.Assignee = user;
                ticket.Team = team;
                return ticket;
            },
            new { Id = id },
            splitOn: "id,id,id");

        return result.FirstOrDefault();
    }

    public async Task<Ticket?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await GetByIdAsync(id, ct);
        if (ticket is null) return null;

        const string commentSql = @"
            SELECT co.*, u.*, cu.*
            FROM comments co
            LEFT JOIN users u       ON u.id = co.author_user_id
            LEFT JOIN customers cu  ON cu.id = co.author_customer_id
            WHERE co.ticket_id = @Id AND co.deleted_at IS NULL
            ORDER BY co.created_at";

        var comments = await _db.QueryAsync<Comment, User, Customer, Comment>(
            commentSql,
            (comment, user, customer) =>
            {
                comment.AuthorUser = user;
                comment.AuthorCustomer = customer;
                return comment;
            },
            new { Id = id },
            splitOn: "id,id");

        ticket.Comments = comments.ToList();
        return ticket;
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM tickets WHERE deleted_at IS NULL ORDER BY created_at DESC";
        return await _db.QueryAsync<Ticket>(sql);
    }

    public async Task<PagedResult<TicketSummary>> SearchAsync(TicketSearchQuery query, CancellationToken ct = default)
    {
        var conditions = new List<string> { "t.deleted_at IS NULL" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            conditions.Add("(t.subject ILIKE @Search OR c.email ILIKE @Search OR c.full_name ILIKE @Search)");
            parameters.Add("Search", $"%{query.Search}%");
        }

        if (query.Status.HasValue)
        {
            conditions.Add("t.status = @Status::ticket_status");
            parameters.Add("Status", query.Status.Value.ToString().ToLower());
        }

        if (query.Priority.HasValue)
        {
            conditions.Add("t.priority = @Priority::ticket_priority");
            parameters.Add("Priority", query.Priority.Value.ToString().ToLower());
        }

        if (query.AssigneeId.HasValue)
        {
            conditions.Add("t.assignee_id = @AssigneeId");
            parameters.Add("AssigneeId", query.AssigneeId.Value);
        }

        if (query.TeamId.HasValue)
        {
            conditions.Add("t.team_id = @TeamId");
            parameters.Add("TeamId", query.TeamId.Value);
        }

        if (query.SlaBreached.HasValue)
        {
            conditions.Add("t.sla_breached = @SlaBreached");
            parameters.Add("SlaBreached", query.SlaBreached.Value);
        }

        var where = string.Join(" AND ", conditions);
        var allowedSortFields = new HashSet<string> { "created_at", "updated_at", "priority", "status", "ticket_number" };
        var sortField = allowedSortFields.Contains(query.SortBy) ? query.SortBy : "created_at";
        var sortDir = query.SortDesc ? "DESC" : "ASC";
        var offset = (query.Page - 1) * query.PageSize;

        var countSql = $@"
            SELECT COUNT(*)
            FROM tickets t
            JOIN customers c ON c.id = t.customer_id
            WHERE {where}";

        var dataSql = $@"
            SELECT
                t.id, t.ticket_number, t.subject, t.status::text AS status, t.priority::text AS priority,
                t.sla_breached, t.created_at, t.updated_at,
                t.first_response_due_at, t.resolution_due_at,
                c.full_name AS customer_name, c.email AS customer_email,
                u.full_name AS assignee_name,
                tm.name     AS team_name,
                cat.name    AS category_name,
                CASE
                    WHEN t.status NOT IN ('resolved','closed') AND t.resolution_due_at IS NOT NULL
                    THEN EXTRACT(EPOCH FROM (t.resolution_due_at - NOW())) / 60
                END AS resolution_minutes_remaining,
                CASE
                    WHEN t.resolution_due_at IS NOT NULL AND t.resolved_at IS NOT NULL
                    THEN ROUND(
                        100.0 * EXTRACT(EPOCH FROM (t.resolution_due_at - t.resolved_at))
                              / EXTRACT(EPOCH FROM (t.resolution_due_at - t.created_at)), 1)
                END AS sla_compliance_pct
            FROM tickets t
            JOIN customers c         ON c.id = t.customer_id
            LEFT JOIN users u        ON u.id = t.assignee_id
            LEFT JOIN teams tm       ON tm.id = t.team_id
            LEFT JOIN categories cat ON cat.id = t.category_id
            WHERE {where}
            ORDER BY t.{sortField} {sortDir}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", offset);

        var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await _db.QueryAsync<TicketSummary>(dataSql, parameters);

        return new PagedResult<TicketSummary>(items, total, query.Page, query.PageSize);
    }

    public async Task<Ticket> AddAsync(Ticket entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO tickets
                (id, subject, description, status, priority, channel,
                 customer_id, assignee_id, team_id, category_id, sla_policy_id,
                 first_response_due_at, resolution_due_at, external_ref, metadata, created_at, updated_at)
            VALUES
                (@Id, @Subject, @Description, @Status::ticket_status, @Priority::ticket_priority,
                 @Channel::ticket_channel, @CustomerId, @AssigneeId, @TeamId, @CategoryId, @SlaPolicyId,
                 @FirstResponseDueAt, @ResolutionDueAt, @ExternalRef, @Metadata::jsonb, @CreatedAt, @UpdatedAt)
            RETURNING ticket_number";

        entity.TicketNumber = await _db.ExecuteScalarAsync<long>(sql, new
        {
            entity.Id, entity.Subject, entity.Description,
            Status   = entity.Status.ToString().ToLower(),
            Priority = entity.Priority.ToString().ToLower(),
            Channel  = entity.Channel.ToString().ToLower(),
            entity.CustomerId, entity.AssigneeId, entity.TeamId,
            entity.CategoryId, entity.SlaPolicyId,
            entity.FirstResponseDueAt, entity.ResolutionDueAt,
            entity.ExternalRef,
            Metadata = entity.Metadata ?? "{}",
            entity.CreatedAt, entity.UpdatedAt
        });

        return entity;
    }

    public async Task UpdateAsync(Ticket entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE tickets SET
                subject = @Subject, description = @Description,
                status = @Status::ticket_status, priority = @Priority::ticket_priority,
                assignee_id = @AssigneeId, team_id = @TeamId, category_id = @CategoryId,
                first_responded_at = @FirstRespondedAt, resolved_at = @ResolvedAt,
                closed_at = @ClosedAt, sla_breached = @SlaBreached, updated_at = @UpdatedAt
            WHERE id = @Id";

        await _db.ExecuteAsync(sql, new
        {
            entity.Id, entity.Subject, entity.Description,
            Status   = entity.Status.ToString().ToLower(),
            Priority = entity.Priority.ToString().ToLower(),
            entity.AssigneeId, entity.TeamId, entity.CategoryId,
            entity.FirstRespondedAt, entity.ResolvedAt,
            entity.ClosedAt, entity.SlaBreached, entity.UpdatedAt
        });
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE tickets SET deleted_at = NOW(), updated_at = NOW() WHERE id = @Id";
        await _db.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<Ticket>> GetOverdueSlaTicketsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT t.*, u.email AS assignee_email
            FROM tickets t
            LEFT JOIN users u ON u.id = t.assignee_id
            WHERE t.deleted_at IS NULL
              AND t.sla_breached = FALSE
              AND t.status NOT IN ('resolved', 'closed')
              AND (
                    t.resolution_due_at < NOW()
                 OR (t.first_response_due_at < NOW() AND t.first_responded_at IS NULL)
              )";

        return await _db.QueryAsync<Ticket>(sql);
    }

    public async Task<IEnumerable<Ticket>> GetByAssigneeAsync(Guid assigneeId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM tickets
            WHERE assignee_id = @AssigneeId
              AND deleted_at IS NULL
              AND status NOT IN ('resolved','closed')
            ORDER BY priority DESC, created_at";

        return await _db.QueryAsync<Ticket>(sql, new { AssigneeId = assigneeId });
    }

    public async Task AssignTagsAsync(Guid ticketId, IEnumerable<Guid> tagIds, CancellationToken ct = default)
    {
        await _db.ExecuteAsync("DELETE FROM ticket_tags WHERE ticket_id = @TicketId", new { TicketId = ticketId });

        foreach (var tagId in tagIds)
            await _db.ExecuteAsync(
                "INSERT INTO ticket_tags (ticket_id, tag_id) VALUES (@TicketId, @TagId) ON CONFLICT DO NOTHING",
                new { TicketId = ticketId, TagId = tagId });
    }
}
