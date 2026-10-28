using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Tickets;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("tickets")]
[Authorize(Roles = Policies.TicketHolders)]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await _tickets.GetMineAsync(cancellationToken));

    // Le QR est un secret : on interdit toute mise en cache
    [HttpGet("{id:guid}/qr")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetQr(Guid id, CancellationToken cancellationToken)
    {
        var png = await _tickets.GetQrAsync(id, cancellationToken);
        return File(png, "image/png");
    }
}