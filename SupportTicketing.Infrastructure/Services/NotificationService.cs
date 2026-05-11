using Microsoft.Extensions.Logging;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Interfaces;

namespace SupportTicketing.Infrastructure.Services;

/// <summary>
/// Stub notification service — logs events.
/// Replace with email/SMS provider (SendGrid, Twilio, etc.) in production.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
        => _logger = logger;

    public Task SendAsync(Notification notification, CancellationToken ct = default)
    {
        _logger.LogInformation("Notification to user {UserId}: {Body}", notification.UserId, notification.Body);
        return Task.CompletedTask;
    }

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        _logger.LogInformation("Ticket created: #{Number} - {Subject}", ticket.TicketNumber, ticket.Subject);
        return Task.CompletedTask;
    }

    public Task SendTicketAssignedAsync(Ticket ticket, CancellationToken ct = default)
    {
        _logger.LogInformation("Ticket #{Number} assigned to {AssigneeId}", ticket.TicketNumber, ticket.AssigneeId);
        return Task.CompletedTask;
    }

    public Task SendSlaWarningAsync(Ticket ticket, CancellationToken ct = default)
    {
        _logger.LogWarning("SLA breach warning for ticket #{Number}", ticket.TicketNumber);
        return Task.CompletedTask;
    }

    public Task SendCsatSurveyAsync(Ticket ticket, CancellationToken ct = default)
    {
        _logger.LogInformation("CSAT survey triggered for ticket #{Number}", ticket.TicketNumber);
        return Task.CompletedTask;
    }
}
