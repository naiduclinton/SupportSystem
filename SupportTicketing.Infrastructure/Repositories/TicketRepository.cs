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
                t.metadata, t.account_holder, t.channel_partner_name,
                t.account_customer, t.account_product
            FROM tickets t
            WHERE t.id = @Id AND t.deleted_at IS NULL";

        var row = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row == null) return null;

        return MapTicket(row);
    }

    private static Ticket MapTicket(dynamic row) { return new Ticket
    {
        Id                  = row.id,
        TicketNumber        = row.ticket_number ?? 0,
        Subject             = row.subject ?? string.Empty,
        Description         = row.description,
        Status              = Enum.TryParse<Core.Enums.TicketStatus>(
                                  ((string)(row.status ?? "open")).Replace("_",""), true, out var s) ? s : Core.Enums.TicketStatus.Open,
        Priority            = Enum.TryParse<Core.Enums.TicketPriority>(
                                  ((string)(row.priority ?? "medium")), true, out var p) ? p : Core.Enums.TicketPriority.Medium,
        Channel             = Enum.TryParse<Core.Enums.TicketChannel>(
                                  ((string)(row.channel ?? "portal")), true, out var ch) ? ch : Core.Enums.TicketChannel.Portal,
        CustomerId          = row.customer_id ?? Guid.Empty,
        AssigneeId          = row.assignee_id,
        TeamId              = row.team_id,
        CategoryId          = row.category_id,
        SlaPolicyId         = row.sla_policy_id,
        FirstResponseDueAt  = row.first_response_due_at,
        ResolutionDueAt     = row.resolution_due_at,
        FirstRespondedAt    = row.first_responded_at,
        ResolvedAt          = row.resolved_at,
        ClosedAt            = row.closed_at,
        SlaBreached         = row.sla_breached ?? false,
        ExternalRef         = row.external_ref,
        Metadata            = row.metadata ?? "{}",
        AccountHolder       = row.account_holder,
        ChannelPartnerName  = row.channel_partner_name,
        AccountCustomer     = row.account_customer,
        AccountProduct      = row.account_product,
        CreatedAt           = row.created_at ?? DateTime.UtcNow,
        UpdatedAt           = row.updated_at ?? DateTime.UtcNow,
        }; }

    public async Task<Ticket?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await GetByIdAsync(id, ct);
        if (ticket is null) return null;

        // Load customer
        var customer = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, email, full_name, phone, company FROM customers WHERE id = @Id",
            new { Id = ticket.CustomerId });
        if (customer != null)
        {
            ticket.Customer = new Core.Entities.Customer
            {
                Id       = customer.id,
                Email    = customer.email ?? string.Empty,
                FullName = customer.full_name,
                Phone    = customer.phone,
                Company  = customer.company,
            };
        }

        const string commentSql = @"
            SELECT
                co.id, co.ticket_id, co.author_user_id, co.author_customer_id,
                co.comment_type::text AS comment_type, co.body, co.is_edited,
                co.created_at, co.updated_at,
                u.id AS u_id, u.full_name AS u_full_name, u.email AS u_email,
                cu.id AS cu_id, cu.full_name AS cu_full_name, cu.email AS cu_email
            FROM comments co
            LEFT JOIN users u       ON u.id = co.author_user_id
            LEFT JOIN customers cu  ON cu.id = co.author_customer_id
            WHERE co.ticket_id = @Id AND co.deleted_at IS NULL
            ORDER BY co.created_at";

        var rows = await _db.QueryAsync<dynamic>(commentSql, new { Id = id });
        ticket.Comments = rows.Select(r => new Comment
        {
            Id               = r.id,
            TicketId         = r.ticket_id,
            AuthorUserId     = r.author_user_id,
            AuthorCustomerId = r.author_customer_id,
            CommentType      = Enum.TryParse<Core.Enums.CommentType>(
                                   ((string)(r.comment_type ?? "reply")).Replace("_",""), true, out var ct2)
                                   ? ct2 : Core.Enums.CommentType.Reply,
            Body             = r.body ?? string.Empty,
            IsEdited         = r.is_edited ?? false,
            CreatedAt        = r.created_at ?? DateTime.UtcNow,
            UpdatedAt        = r.updated_at ?? DateTime.UtcNow,
            AuthorUser       = r.u_id == null ? null : new Core.Entities.User
                               { Id = r.u_id, FullName = r.u_full_name ?? "", Email = r.u_email ?? "" },
            AuthorCustomer   = r.cu_id == null ? null : new Core.Entities.Customer
                               { Id = r.cu_id, FullName = r.cu_full_name, Email = r.cu_email ?? "" },
        }).ToList();

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
            parameters.Add("Status", ToSnakeCase(query.Status.Value.ToString()));
        }

        if (query.Priority.HasValue)
        {
            conditions.Add("t.priority = @Priority::ticket_priority");
            parameters.Add("Priority", ToSnakeCase(query.Priority.Value.ToString()));
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
        var rows = await _db.QueryAsync<dynamic>(dataSql, parameters);
        var items = rows.Select(r => new TicketSummary
        {
            Id                         = r.id,
            TicketNumber               = r.ticket_number ?? 0,
            Subject                    = r.subject ?? string.Empty,
            Status                     = r.status ?? "open",
            Priority                   = r.priority ?? "medium",
            CustomerName               = r.customer_name ?? string.Empty,
            CustomerEmail              = r.customer_email ?? string.Empty,
            AssigneeName               = r.assignee_name,
            TeamName                   = r.team_name,
            CategoryName               = r.category_name,
            SlaBreached                = r.sla_breached ?? false,
            SlaCompliancePct           = r.sla_compliance_pct == null ? (double?)null : (double)r.sla_compliance_pct,
            ResolutionMinutesRemaining = r.resolution_minutes_remaining == null ? (double?)null : (double)r.resolution_minutes_remaining,
            CreatedAt                  = r.created_at ?? DateTime.UtcNow,
            UpdatedAt                  = r.updated_at ?? DateTime.UtcNow,
        }).ToList();

        return new PagedResult<TicketSummary>(items, total, query.Page, query.PageSize);
    }

    public async Task<Ticket> AddAsync(Ticket entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO tickets
                (id, subject, description, status, priority, channel,
                 customer_id, assignee_id, team_id, category_id, sla_policy_id,
                 first_response_due_at, resolution_due_at, external_ref, metadata,
                 account_holder, channel_partner_name, account_customer, account_product,
                 created_at, updated_at)
            VALUES
                (@Id, @Subject, @Description, @Status::ticket_status, @Priority::ticket_priority,
                 @Channel::ticket_channel, @CustomerId, @AssigneeId, @TeamId, @CategoryId, @SlaPolicyId,
                 @FirstResponseDueAt, @ResolutionDueAt, @ExternalRef, @Metadata::jsonb,
                 @AccountHolder, @ChannelPartnerName, @AccountCustomer, @AccountProduct,
                 @CreatedAt, @UpdatedAt)
            RETURNING id, ticket_number, created_at, updated_at";

        var result = await _db.QueryFirstAsync<dynamic>(sql, new
        {
            entity.Id, entity.Subject, entity.Description,
            Status   = ToSnakeCase(entity.Status.ToString()),
            Priority = ToSnakeCase(entity.Priority.ToString()),
            Channel  = ToSnakeCase(entity.Channel.ToString()),
            entity.CustomerId, entity.AssigneeId, entity.TeamId,
            entity.CategoryId, entity.SlaPolicyId,
            entity.FirstResponseDueAt, entity.ResolutionDueAt,
            entity.ExternalRef,
            Metadata = entity.Metadata ?? "{}",
            entity.CreatedAt, entity.UpdatedAt,
            entity.AccountHolder, entity.ChannelPartnerName,
            entity.AccountCustomer, entity.AccountProduct
        });
        entity.TicketNumber = result.ticket_number;

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
            Status   = ToSnakeCase(entity.Status.ToString()),
            Priority = ToSnakeCase(entity.Priority.ToString()),
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
            SELECT
                t.id, t.ticket_number, t.subject, t.description,
                t.status::text AS status, t.priority::text AS priority,
                t.channel::text AS channel, t.customer_id, t.assignee_id,
                t.team_id, t.sla_policy_id, t.first_response_due_at,
                t.resolution_due_at, t.first_responded_at, t.resolved_at,
                t.closed_at, t.sla_breached, t.created_at, t.updated_at,
                t.metadata, u.email AS assignee_email
            FROM tickets t
            LEFT JOIN users u ON u.id = t.assignee_id
            WHERE t.deleted_at IS NULL
              AND t.sla_breached = FALSE
              AND t.status NOT IN ('resolved', 'closed')
              AND (
                    t.resolution_due_at < NOW()
                 OR (t.first_response_due_at < NOW() AND t.first_responded_at IS NULL)
              )";

        var rows = await _db.QueryAsync<dynamic>(sql);
        var tickets = new List<Ticket>();
        foreach (var r in rows) tickets.Add(MapTicket(r));
        return tickets;
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

    private static string ToSnakeCase(string s)
        => System.Text.RegularExpressions.Regex.Replace(s, "([A-Z])", "_$1").TrimStart('_').ToLower();

    public async Task AssignTagsAsync(Guid ticketId, IEnumerable<Guid> tagIds, CancellationToken ct = default)
    {
        await _db.ExecuteAsync("DELETE FROM ticket_tags WHERE ticket_id = @TicketId", new { TicketId = ticketId });

        foreach (var tagId in tagIds)
            await _db.ExecuteAsync(
                "INSERT INTO ticket_tags (ticket_id, tag_id) VALUES (@TicketId, @TagId) ON CONFLICT DO NOTHING",
                new { TicketId = ticketId, TagId = tagId });
    }
}
