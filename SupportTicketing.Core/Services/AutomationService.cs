using Microsoft.Extensions.Logging;
using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Interfaces;

namespace SupportTicketing.Core.Services;

public class AutomationService : IAutomationService
{
    private readonly IAutomationRuleRepository _rules;
    private readonly ITicketRepository _tickets;
    private readonly INotificationService _notifications;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(
        IAutomationRuleRepository rules,
        ITicketRepository tickets,
        INotificationService notifications,
        ILogger<AutomationService> logger)
    {
        _rules = rules;
        _tickets = tickets;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ExecuteAsync(AutomationTrigger trigger, Ticket ticket, CancellationToken ct = default)
    {
        var rules = await _rules.GetByTriggerAsync(trigger, ct);

        foreach (var rule in rules.OrderBy(r => r.ExecutionOrder))
        {
            if (!EvaluateConditions(rule.Conditions, ticket)) continue;

            _logger.LogInformation("Applying automation rule '{Rule}' to ticket #{Number}", rule.Name, ticket.TicketNumber);

            foreach (var action in rule.Actions)
                await ApplyActionAsync(action, ticket, ct);
        }
    }

    private static bool EvaluateConditions(List<AutomationCondition> conditions, Ticket ticket)
    {
        // All conditions must pass (AND logic)
        return conditions.All(c => EvaluateCondition(c, ticket));
    }

    private static bool EvaluateCondition(AutomationCondition condition, Ticket ticket)
    {
        var fieldValue = GetFieldValue(ticket, condition.Field)?.ToString() ?? string.Empty;

        return condition.Operator switch
        {
            "eq"       => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "neq"      => !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            _          => false
        };
    }

    private static object? GetFieldValue(Ticket ticket, string field) => field.ToLower() switch
    {
        "priority" => ticket.Priority.ToString(),
        "status"   => ticket.Status.ToString(),
        "channel"  => ticket.Channel.ToString(),
        "category" => ticket.CategoryId?.ToString(),
        "team"     => ticket.TeamId?.ToString(),
        _          => null
    };

    private async Task ApplyActionAsync(AutomationAction action, Ticket ticket, CancellationToken ct)
    {
        switch (action.Action)
        {
            case "assign_agent" when Guid.TryParse(action.Value, out var agentId):
                ticket.AssigneeId = agentId;
                await _tickets.UpdateAsync(ticket, ct);
                break;

            case "assign_team" when Guid.TryParse(action.Value, out var teamId):
                ticket.TeamId = teamId;
                await _tickets.UpdateAsync(ticket, ct);
                break;

            case "set_priority" when Enum.TryParse<TicketPriority>(action.Value, true, out var priority):
                ticket.Priority = priority;
                await _tickets.UpdateAsync(ticket, ct);
                break;

            case "set_status" when Enum.TryParse<TicketStatus>(action.Value, true, out var status):
                ticket.Status = status;
                await _tickets.UpdateAsync(ticket, ct);
                break;

            case "escalate":
                if (ticket.Priority < TicketPriority.Critical)
                {
                    ticket.Priority = (TicketPriority)((int)ticket.Priority + 1);
                    await _tickets.UpdateAsync(ticket, ct);
                }
                break;

            default:
                _logger.LogWarning("Unknown automation action: {Action}", action.Action);
                break;
        }
    }
}
