using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Events;

public record SaveEventRequest(
    string Title,
    string Location,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Description = null);

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    Guid OrganizerId,
    DateTime? OriginalStartDate,
    DateTime? OriginalEndDate,
    string? PostponementReason);

// Filtres de la liste, lus dans l'URL : /events?search=jazz&page=2
public record EventQuery(
    string? Search = null,
    string? Location = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
