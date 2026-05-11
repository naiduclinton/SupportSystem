using Microsoft.Extensions.Logging;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Core.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;
    private readonly ISlaPolicyRepository _slaPolicies;
    private readonly ISlaService _slaService;
    private readonly INotificationService _notifications;
    private readonly IAutomationService _automation;
    private readonly IMessageBus _bus;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository tickets,
        ICustomerRepository customers,
        ISlaPolicyRepository slaPolicies,
        ISlaService slaService,
        INotificationService notifications,
        IAutomationService automation,
        IMessageBus bus,
        ILogger<TicketService> logger)
    {
        _tickets = tickets;
        _customers = customers;
        _slaPolicies = slaPolicies;
        _slaService = slaService;
        _notifications = notifications;
        _automation = automation;
        _bus = bus;
        _logger = logger;
    }

    public async Task<Ticket> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetOrCreateAsync(request.CustomerEmail, request.CustomerName, ct);

        var slaPolicy = await _slaPolicies.GetDefaultForPriorityAsync(request.Priority, ct);

        var ticket = new Ticket
        {
            Subject = request.Subject,
            Description = request.Description,
            Priority = request.Priority,
            Channel = request.Channel,
            CustomerId = customer.Id,
            AssigneeId = request.AssigneeId,
            TeamId = request.TeamId,
            CategoryId = request.CategoryId,
            SlaPolicyId = slaPolicy?.Id,
            Status = TicketStatus.Open
        };

        if (slaPolicy is not null)
        {
            var (firstDue, resDue) = await _slaService.CalculateDueDatesAsync(slaPolicy.Id, ticket.CreatedAt, ct);
            ticket.FirstResponseDueAt = firstDue;
            ticket.ResolutionDueAt = resDue;
        }

        await _tickets.AddAsync(ticket, ct);

        _logger.LogInformation("Ticket #{Number} created for customer {Email}", ticket.TicketNumber, customer.Email);

        // Publish event to RabbitMQ
        await _bus.PublishAsync(
            "tickets",
            "ticket.created",
            new TicketCreatedEvent(ticket.Id, ticket.TicketNumber, ticket.Subject, customer.Email, ticket.AssigneeId),
            ct);

        // Fire automation rules (non-fatal)
        try { await _automation.ExecuteAsync(AutomationTrigger.TicketCreated, ticket, ct); }
        catch (Exception ex) { _logger.LogWarning("Automation failed: {Error}", ex.Message); }

        // Send notifications
        await _notifications.SendTicketCreatedAsync(ticket, ct);

        return ticket;
    }

    public async Task<Ticket> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");
        var oldStatus = ticket.Status;
        ticket.Status = request.Status;

        if (request.Status == TicketStatus.Resolved && ticket.ResolvedAt is null)
            ticket.ResolvedAt = DateTime.UtcNow;

        if (request.Status == TicketStatus.Closed && ticket.ClosedAt is null)
            ticket.ClosedAt = DateTime.UtcNow;

        await _tickets.UpdateAsync(ticket, ct);

        await _bus.PublishAsync(
            "tickets",
            "ticket.status_changed",
            new TicketStatusChangedEvent(ticket.Id, ticket.TicketNumber, oldStatus, ticket.Status),
            ct);

        try { await _automation.ExecuteAsync(AutomationTrigger.StatusChanged, ticket, ct); }
        catch (Exception ex) { _logger.LogWarning("Automation failed: {Error}", ex.Message); }

        // Send CSAT survey on resolution
        if (request.Status == TicketStatus.Resolved)
            await _notifications.SendCsatSurveyAsync(ticket, ct);

        return ticket;
    }

    public async Task<Ticket> AssignAsync(Guid id, AssignTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");

        ticket.AssigneeId = request.AssigneeId;
        ticket.TeamId = request.TeamId;

        if (ticket.Status == TicketStatus.Open && request.AssigneeId.HasValue)
            ticket.Status = TicketStatus.InProgress;

        await _tickets.UpdateAsync(ticket, ct);

        if (request.AssigneeId.HasValue)
            await _notifications.SendTicketAssignedAsync(ticket, ct);

        return ticket;
    }

    public async Task<Comment> AddCommentAsync(Guid ticketId, AddCommentRequest request, CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var comment = new Comment
        {
            TicketId = ticketId,
            AuthorUserId = request.AuthorUserId,
            AuthorCustomerId = request.AuthorCustomerId,
            CommentType = request.CommentType,
            Body = request.Body
        };

        // Track first response time
        if (request.AuthorUserId.HasValue &&
            request.CommentType == CommentType.Reply &&
            ticket.FirstRespondedAt is null)
        {
            ticket.FirstRespondedAt = DateTime.UtcNow;
            await _tickets.UpdateAsync(ticket, ct);
        }

        await _bus.PublishAsync("tickets", "ticket.updated",
            new TicketStatusChangedEvent(ticket.Id, ticket.TicketNumber, ticket.Status, ticket.Status), ct);

        return comment;
    }
}
