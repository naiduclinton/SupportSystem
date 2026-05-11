using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketing.Core.Enums;
using SupportTicketing.Core.Interfaces;
using SupportTicketing.Core.Models;

namespace SupportTicketing.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[AllowAnonymous]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketRepository _ticketRepo;

    public TicketsController(ITicketService ticketService, ITicketRepository ticketRepo)
    {
        _ticketService = ticketService;
        _ticketRepo = ticketRepo;
    }

    /// <summary>Search and list tickets with filtering, sorting, and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketSummary>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] Guid? assigneeId,
        [FromQuery] Guid? teamId,
        [FromQuery] bool? slaBreached,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sortBy = "created_at",
        [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        var query = new TicketSearchQuery(
            search, status, priority, assigneeId, teamId, null,
            slaBreached, null, null, page, Math.Min(pageSize, 100), sortBy, sortDesc);

        var result = await _ticketRepo.SearchAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Get a single ticket with full details and comments.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var ticket = await _ticketRepo.GetWithDetailsAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    /// <summary>Create a new ticket.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await _ticketService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    /// <summary>Update ticket status.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        var ticket = await _ticketService.UpdateStatusAsync(id, request, ct);
        return Ok(ticket);
    }

    /// <summary>Assign ticket to an agent or team.</summary>
    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTicketRequest request,
        CancellationToken ct)
    {
        var ticket = await _ticketService.AssignAsync(id, request, ct);
        return Ok(ticket);
    }

    /// <summary>Add a comment or internal note to a ticket.</summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        var comment = await _ticketService.AddCommentAsync(id, request, ct);
        return Created($"/api/tickets/{id}/comments/{comment.Id}", comment);
    }

    /// <summary>Soft-delete a ticket (admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _ticketRepo.SoftDeleteAsync(id, ct);
        return NoContent();
    }
}
