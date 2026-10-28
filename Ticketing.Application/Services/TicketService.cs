using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Tickets;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly IQrCodeGenerator _qrCodes;
    private readonly ICurrentUserService _currentUser;

    public TicketService(
        ITicketRepository tickets, IQrCodeGenerator qrCodes, ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _qrCodes = qrCodes;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        return await _tickets.GetByUserAsync(userId, cancellationToken);
    }

    public async Task<byte[]> GetQrAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        // Le billet d'un autre : « introuvable » plutôt que « interdit »
        var ticket = await _tickets.GetOwnedAsync(ticketId, userId, cancellationToken)
            ?? throw new NotFoundException("Billet introuvable.");

        if (ticket.Status != TicketStatus.Valid)
            throw new DomainException($"Ce billet n'est plus valide (statut « {ticket.Status} »).");

        return _qrCodes.GeneratePng(ticket.Code);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new AuthException("Utilisateur non authentifié.");
}
