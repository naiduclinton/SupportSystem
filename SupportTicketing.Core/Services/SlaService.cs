using Microsoft.Extensions.Logging;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Core.Services;

public class SlaService : ISlaService
{
    private readonly ISlaPolicyRepository _policies;
    private readonly ITicketRepository _tickets;
    private readonly INotificationService _notifications;
    private readonly IMessageBus _bus;
    private readonly ILogger<SlaService> _logger;

    // Business hours: Mon–Fri 08:00–17:00 (configurable via settings in production)
    private static readonly TimeOnly BusinessStart = new(8, 0);
    private static readonly TimeOnly BusinessEnd = new(17, 0);

    public SlaService(
        ISlaPolicyRepository policies,
        ITicketRepository tickets,
        INotificationService notifications,
        IMessageBus bus,
        ILogger<SlaService> logger)
    {
        _policies = policies;
        _tickets = tickets;
        _notifications = notifications;
        _bus = bus;
        _logger = logger;
    }

    public async Task<(DateTime firstResponseDue, DateTime resolutionDue)> CalculateDueDatesAsync(
        Guid slaPolicyId, DateTime createdAt, CancellationToken ct = default)
    {
        var policy = await _policies.GetByIdAsync(slaPolicyId, ct)
            ?? throw new KeyNotFoundException($"SLA policy {slaPolicyId} not found.");

        var firstDue = policy.BusinessHoursOnly
            ? AddBusinessMinutes(createdAt, policy.FirstResponseMinutes)
            : createdAt.AddMinutes(policy.FirstResponseMinutes);

        var resDue = policy.BusinessHoursOnly
            ? AddBusinessMinutes(createdAt, policy.ResolutionMinutes)
            : createdAt.AddMinutes(policy.ResolutionMinutes);

        return (firstDue, resDue);
    }

    public async Task EvaluateBreachesAsync(CancellationToken ct = default)
    {
        var overdueTickets = await _tickets.GetOverdueSlaTicketsAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var ticket in overdueTickets)
        {
            if (ticket.SlaBreached) continue;

            var isBreached =
                (ticket.ResolutionDueAt.HasValue && ticket.ResolutionDueAt < now) ||
                (ticket.FirstResponseDueAt.HasValue && ticket.FirstRespondedAt is null && ticket.FirstResponseDueAt < now);

            if (!isBreached) continue;

            ticket.SlaBreached = true;
            await _tickets.UpdateAsync(ticket, ct);

            _logger.LogWarning("SLA breached for ticket #{Number}", ticket.TicketNumber);

            await _bus.PublishAsync(
                "tickets",
                "ticket.sla_breached",
                new SlaBreachEvent(
                    ticket.Id,
                    ticket.TicketNumber,
                    ticket.Subject,
                    ticket.Priority,
                    ticket.Assignee?.Email),
                ct);

            await _notifications.SendSlaWarningAsync(ticket, ct);
        }
    }

    // ── Business hours calculation ────────────────────────────────────────
    private static DateTime AddBusinessMinutes(DateTime from, int minutes)
    {
        var current = EnsureBusinessHours(from);
        var remaining = minutes;

        while (remaining > 0)
        {
            var endOfDay = current.Date.Add(BusinessEnd.ToTimeSpan());
            var minutesUntilEod = (int)(endOfDay - current).TotalMinutes;

            if (remaining <= minutesUntilEod)
            {
                current = current.AddMinutes(remaining);
                remaining = 0;
            }
            else
            {
                remaining -= minutesUntilEod;
                current = NextBusinessDayStart(current);
            }
        }

        return current;
    }

    private static DateTime EnsureBusinessHours(DateTime dt)
    {
        // Skip weekends
        while (dt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            dt = dt.AddDays(1).Date.Add(BusinessStart.ToTimeSpan());

        var time = TimeOnly.FromDateTime(dt);

        if (time < BusinessStart)
            return dt.Date.Add(BusinessStart.ToTimeSpan());

        if (time >= BusinessEnd)
            return NextBusinessDayStart(dt);

        return dt;
    }

    private static DateTime NextBusinessDayStart(DateTime dt)
    {
        var next = dt.AddDays(1).Date.Add(BusinessStart.ToTimeSpan());
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }
}
