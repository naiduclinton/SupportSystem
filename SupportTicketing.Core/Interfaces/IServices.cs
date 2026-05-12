using SupportTicketing.Core.Entities;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Core.Interfaces;

public interface ITicketService
{
    Task<Ticket> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<Ticket> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task<Ticket> AssignAsync(Guid id, AssignTicketRequest request, CancellationToken ct = default);
    Task<Comment> AddCommentAsync(Guid ticketId, AddCommentRequest request, CancellationToken ct = default);
}

public interface ISlaService
{
    Task<(DateTime firstResponseDue, DateTime resolutionDue)> CalculateDueDatesAsync(
        Guid slaPolicyId, DateTime createdAt, CancellationToken ct = default);
    Task EvaluateBreachesAsync(CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface INotificationService
{
    Task SendAsync(Notification notification, CancellationToken ct = default);
    Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default);
    Task SendTicketAssignedAsync(Ticket ticket, CancellationToken ct = default);
    Task SendSlaWarningAsync(Ticket ticket, CancellationToken ct = default);
    Task SendCsatSurveyAsync(Ticket ticket, CancellationToken ct = default);
}

public interface IAutomationService
{
    Task ExecuteAsync(AutomationTrigger trigger, Ticket ticket, CancellationToken ct = default);
}

public interface IReportingService
{
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<IEnumerable<AgentWorkload>> GetAgentWorkloadsAsync(CancellationToken ct = default);
    Task<SlaReport> GetSlaReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<DashboardDrillDown> GetDashboardDrillDownAsync(CancellationToken ct = default);
}

public interface IMessageBus
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default);
}
