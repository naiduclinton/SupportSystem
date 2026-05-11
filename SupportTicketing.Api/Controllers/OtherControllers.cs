using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Api.Controllers;

// ── Auth ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Login and receive JWT access + refresh tokens.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(request.Email, request.Password, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    /// <summary>Debug login — returns detailed info without actually logging in.</summary>
    [HttpPost("debug-login")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<SupportTicketing.Core.Interfaces.IUserRepository>();
            var user = await userRepo.GetByEmailAsync(request.Email, ct);

            if (user == null)
                return Ok(new { found = false, email = request.Email });

            var hashMatch = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash ?? "");
            return Ok(new {
                found      = true,
                email      = user.Email,
                isActive   = user.IsActive,
                hasHash    = !string.IsNullOrEmpty(user.PasswordHash),
                hashPrefix = user.PasswordHash?.Substring(0, 10),
                passwordMatch = hashMatch,
                role       = user.Role.ToString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Exchange a refresh token for a new access token.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken, CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(refreshToken, ct);
        return Ok(result);
    }
}

// ── Users ─────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IAuthService _auth;
    public UsersController(IUserRepository users, IAuthService auth)
    {
        _users = users;
        _auth = auth;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("workloads")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetWorkloads(CancellationToken ct)
    {
        var workloads = await _users.GetAgentWorkloadsAsync(ct);
        return Ok(workloads);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _users.SoftDeleteAsync(id, ct);
        return NoContent();
    }
}

// ── Reports ───────────────────────────────────────────────────────────────
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Agent")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reporting;
    public ReportsController(IReportingService reporting) => _reporting = reporting;

    /// <summary>Dashboard summary stats.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var stats = await _reporting.GetDashboardStatsAsync(ct);
        return Ok(stats);
    }

    /// <summary>SLA compliance report for a date range.</summary>
    [HttpGet("sla")]
    public async Task<IActionResult> SlaReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var report = await _reporting.GetSlaReportAsync(from, to, ct);
        return Ok(report);
    }

    /// <summary>Agent workload breakdown.</summary>
    [HttpGet("agents")]
    public async Task<IActionResult> AgentWorkloads(CancellationToken ct)
    {
        var workloads = await _reporting.GetAgentWorkloadsAsync(ct);
        return Ok(workloads);
    }
}

// ── Notifications ─────────────────────────────────────────────────────────
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notifications;
    public NotificationsController(INotificationRepository notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var items = await _notifications.GetUnreadByUserAsync(userId, ct);
        return Ok(items);
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _notifications.MarkAllReadAsync(userId, ct);
        return NoContent();
    }
}
