using System.Data;
using Dapper;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly IDbConnection _db;
    public ReportingService(IDbConnection db) => _db = db;

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var stats = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                COUNT(*) FILTER (WHERE status = 'open')        AS open_count,
                COUNT(*) FILTER (WHERE status = 'in_progress') AS in_progress_count,
                COUNT(*) FILTER (WHERE status = 'pending')     AS pending_count,
                COUNT(*) FILTER (WHERE status = 'resolved'
                    AND resolved_at::date = CURRENT_DATE)       AS resolved_today_count,
                COUNT(*) FILTER (WHERE sla_breached = TRUE
                    AND status NOT IN ('resolved','closed'))     AS sla_breach_count,
                ROUND(AVG(EXTRACT(EPOCH FROM (first_responded_at - created_at))/3600)
                    FILTER (WHERE first_responded_at IS NOT NULL), 2) AS avg_first_response_hours,
                ROUND(AVG(EXTRACT(EPOCH FROM (resolved_at - created_at))/3600)
                    FILTER (WHERE resolved_at IS NOT NULL), 2)  AS avg_resolution_hours
            FROM tickets WHERE deleted_at IS NULL");

        var csat = await _db.ExecuteScalarAsync<double?>(
            "SELECT ROUND(AVG(score) * 20, 1) FROM csat_surveys WHERE responded_at IS NOT NULL") ?? 0;

        return new DashboardStats(
            OpenCount:             (int)(stats?.open_count ?? 0),
            InProgressCount:       (int)(stats?.in_progress_count ?? 0),
            PendingCount:          (int)(stats?.pending_count ?? 0),
            ResolvedTodayCount:    (int)(stats?.resolved_today_count ?? 0),
            AvgFirstResponseHours: (double)(stats?.avg_first_response_hours ?? 0),
            AvgResolutionHours:    (double)(stats?.avg_resolution_hours ?? 0),
            CsatScore:             csat,
            SlaBreachCount:        (int)(stats?.sla_breach_count ?? 0)
        );
    }

    public async Task<IEnumerable<AgentWorkload>> GetAgentWorkloadsAsync(CancellationToken ct = default)
    {
        var rows = await _db.QueryAsync<dynamic>(@"
            SELECT
                u.id, u.full_name, tm.name AS team_name,
                COUNT(t.id) FILTER (WHERE t.status = 'open')        AS open_count,
                COUNT(t.id) FILTER (WHERE t.status = 'in_progress') AS in_progress_count,
                COUNT(t.id) FILTER (WHERE t.status = 'pending')     AS pending_count,
                COUNT(t.id) FILTER (WHERE t.sla_breached = TRUE
                    AND t.status NOT IN ('resolved','closed'))       AS sla_breach_count,
                ROUND(AVG(EXTRACT(EPOCH FROM (t.resolved_at - t.created_at))/3600)
                    FILTER (WHERE t.resolved_at IS NOT NULL), 2)     AS avg_resolution_hours
            FROM users u
            LEFT JOIN teams tm   ON tm.id = u.team_id
            LEFT JOIN tickets t  ON t.assignee_id = u.id AND t.deleted_at IS NULL
            WHERE u.deleted_at IS NULL AND u.is_active = TRUE
            GROUP BY u.id, u.full_name, tm.name
            ORDER BY u.full_name");

        return rows.Select(r => new AgentWorkload(
            UserId:             r.id,
            FullName:           r.full_name,
            TeamName:           r.team_name,
            OpenCount:          (int)(r.open_count ?? 0),
            InProgressCount:    (int)(r.in_progress_count ?? 0),
            PendingCount:       (int)(r.pending_count ?? 0),
            SlaBreachCount:     (int)(r.sla_breach_count ?? 0),
            AvgResolutionHours: r.avg_resolution_hours
        ));
    }

    public async Task<DashboardDrillDown> GetDashboardDrillDownAsync(CancellationToken ct = default)
    {
        // Volume by day — last 7 days
        var volumeRows = await _db.QueryAsync<dynamic>(@"
            SELECT
                TO_CHAR(d.day, 'Dy') AS day_label,
                TO_CHAR(d.day, 'YYYY-MM-DD') AS date,
                COUNT(t.id) FILTER (WHERE t.created_at::date = d.day::date)  AS created,
                COUNT(t.id) FILTER (WHERE t.resolved_at::date = d.day::date) AS resolved
            FROM generate_series(
                CURRENT_DATE - INTERVAL '6 days',
                CURRENT_DATE,
                '1 day'::interval
            ) AS d(day)
            LEFT JOIN tickets t ON t.deleted_at IS NULL
            GROUP BY d.day
            ORDER BY d.day");

        var volume = volumeRows.Select(r => new TicketVolumePoint(
            Day:      (string)(r.day_label ?? ""),
            Date:     (string)(r.date ?? ""),
            Created:  (int)(r.created ?? 0),
            Resolved: (int)(r.resolved ?? 0)
        )).ToList();

        // By category
        var total = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tickets WHERE deleted_at IS NULL AND status != 'closed'");
        var categoryRows = await _db.QueryAsync<dynamic>(@"
            SELECT
                COALESCE(c.name, 'Uncategorized') AS category,
                COUNT(t.id) AS count
            FROM tickets t
            LEFT JOIN categories c ON c.id = t.category_id
            WHERE t.deleted_at IS NULL AND t.status != 'closed'
            GROUP BY c.name
            ORDER BY count DESC");

        var categories = categoryRows.Select(r => new CategoryBreakdown(
            Category:   (string)(r.category ?? "Uncategorized"),
            Count:      (int)(r.count ?? 0),
            Percentage: total > 0 ? Math.Round(100.0 * (int)(r.count ?? 0) / total, 1) : 0
        )).ToList();

        // Recent tickets (last 10)
        var recentRows = await _db.QueryAsync<dynamic>(@"
            SELECT
                t.id, t.ticket_number, t.subject,
                t.status::text AS status, t.priority::text AS priority,
                t.sla_breached, t.created_at, t.updated_at,
                t.first_response_due_at, t.resolution_due_at,
                c.full_name AS customer_name, c.email AS customer_email,
                u.full_name AS assignee_name,
                tm.name AS team_name, cat.name AS category_name,
                NULL::numeric AS sla_compliance_pct,
                NULL::numeric AS resolution_minutes_remaining
            FROM tickets t
            JOIN customers c         ON c.id = t.customer_id
            LEFT JOIN users u        ON u.id = t.assignee_id
            LEFT JOIN teams tm       ON tm.id = t.team_id
            LEFT JOIN categories cat ON cat.id = t.category_id
            WHERE t.deleted_at IS NULL
            ORDER BY t.created_at DESC
            LIMIT 10");

        var recent = recentRows.Select(r => new TicketSummary
        {
            Id           = r.id,
            TicketNumber = r.ticket_number ?? 0,
            Subject      = r.subject ?? string.Empty,
            Status       = r.status ?? "open",
            Priority     = r.priority ?? "medium",
            CustomerName  = r.customer_name ?? string.Empty,
            CustomerEmail = r.customer_email ?? string.Empty,
            AssigneeName  = r.assignee_name,
            TeamName      = r.team_name,
            CategoryName  = r.category_name,
            SlaBreached   = r.sla_breached ?? false,
            CreatedAt     = r.created_at ?? DateTime.UtcNow,
            UpdatedAt     = r.updated_at ?? DateTime.UtcNow,
        }).ToList();

        return new DashboardDrillDown(volume, categories, recent);
    }

    public async Task<SlaReport> GetSlaReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var total = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tickets WHERE created_at BETWEEN @From AND @To AND deleted_at IS NULL",
            new { From = from, To = to });

        var breached = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tickets WHERE created_at BETWEEN @From AND @To AND sla_breached = TRUE AND deleted_at IS NULL",
            new { From = from, To = to });

        var compliance = total > 0 ? Math.Round(100.0 * (total - breached) / total, 1) : 100.0;

        var breachDetails = await _db.QueryAsync<dynamic>(@"
            SELECT t.id, t.ticket_number, t.subject, t.priority::text AS priority,
                   u.full_name AS assignee_name, t.resolution_due_at, t.resolved_at
            FROM tickets t
            LEFT JOIN users u ON u.id = t.assignee_id
            WHERE t.created_at BETWEEN @From AND @To
              AND t.sla_breached = TRUE AND t.deleted_at IS NULL
            ORDER BY t.resolution_due_at",
            new { From = from, To = to });

        var details = breachDetails.Select(r => new SlaBreachDetail(
            TicketId:     r.id,
            TicketNumber: r.ticket_number,
            Subject:      r.subject,
            Priority:     Enum.Parse<SupportTicketing.Core.Enums.TicketPriority>(r.priority, true),
            AssigneeName: r.assignee_name,
            DueAt:        r.resolution_due_at,
            ResolvedAt:   r.resolved_at
        ));

        return new SlaReport(from, to, total, breached, compliance, details);
    }
}
