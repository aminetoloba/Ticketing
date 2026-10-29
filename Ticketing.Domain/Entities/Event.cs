using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;
using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class Event : AuditableEntity
{
    private Event() { }

    public static readonly TimeSpan CheckInOpensBefore = TimeSpan.FromHours(3);
    public static readonly TimeSpan CheckInClosesAfter = TimeSpan.FromHours(3);
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public EventStatus Status { get; private set; }
    public Guid OrganizerId { get; private set; }
    private readonly List<TicketType> _ticketTypes = new();
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes;

    public DateTime? OriginalStartDate { get; private set; }
    public DateTime? OriginalEndDate { get; private set; }
    public string? PostponementReason { get; private set; }
    public DateTime? PostponedAt { get; private set; }

    // Après un report, les participants peuvent demander un remboursement pendant cette durée
    public static readonly TimeSpan RefundWindow = TimeSpan.FromDays(14);

    public DateTime? RefundWindowEndsAt => PostponedAt + RefundWindow;

    public static Event Create(
        string title,
        string description,
        string location,
        DateTime startDate,
        DateTime endDate,
        Guid organizerId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Le titre est obligatoire.");
        if (endDate <= startDate)
            throw new DomainException("La date de fin doit être après la date de début.");

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description,
            Location = location,
            StartDate = startDate,
            EndDate = endDate,
            OrganizerId = organizerId,
            Status = EventStatus.Draft
        };
    }

    public void Update(
        string title,
        string description,
        string location,
        DateTime startDate,
        DateTime endDate)
    {
        EnsureStatus("modifier", EventStatus.Draft, EventStatus.Published);

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Le titre est obligatoire.");
        if (endDate <= startDate)
            throw new DomainException("La date de fin doit être après la date de début.");

        // Des billets peuvent déjà être vendus : les dates passent par le report
        if (Status == EventStatus.Published && (startDate != StartDate || endDate != EndDate))
            throw new DomainException(
                "Les dates d'un événement publié ne peuvent changer que par un report.");

        Title = title.Trim();
        Description = description;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Publish(DateTime utcNow)
    {
        EnsureStatus("publier", EventStatus.Draft);

        if (_ticketTypes.Count == 0)
            throw new DomainException(
                "Ajoutez au moins un type de billet avant de publier l'événement.");

        if (StartDate <= utcNow)
            throw new DomainException(
                "Impossible de publier un événement dont la date de début est passée.");

        Status = EventStatus.Published;
    }

    public void Postpone(DateTime newStartDate, DateTime newEndDate, string reason, DateTime utcNow)
    {
        EnsureStatus("reporter", EventStatus.Published);

        if (newEndDate <= newStartDate)
            throw new DomainException("La date de fin doit être après la date de début.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Le motif du report est obligatoire.");
        if (newStartDate <= utcNow)
            throw new DomainException("La nouvelle date de début doit être dans le futur.");
        if (newStartDate == StartDate && newEndDate == EndDate)
            throw new DomainException("Les nouvelles dates sont identiques aux dates actuelles.");

        OriginalStartDate = StartDate;
        OriginalEndDate = EndDate;
        StartDate = newStartDate;
        EndDate = newEndDate;
        PostponementReason = reason.Trim();
        PostponedAt = utcNow;
        Status = EventStatus.Postponed;
    }

    public void Republish()
    {
        EnsureStatus("republier", EventStatus.Postponed);
        Status = EventStatus.Published;
    }

    public void Cancel()
    {
        EnsureStatus("annuler", EventStatus.Published, EventStatus.Postponed);
        Status = EventStatus.Cancelled;
    }

    public void Complete()
    {
        EnsureStatus("terminer", EventStatus.Published);
        Status = EventStatus.Completed;
    }

    public TicketType AddTicketType(string name, decimal price, int totalQuantity)
    {
        if (Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new DomainException(
                $"Impossible d'ajouter un billet à un événement au statut « {Status} ».");

        var ticketType = TicketType.Create(Id, name, price, totalQuantity);
        _ticketTypes.Add(ticketType);
        return ticketType;
    }

    public void EnsureCheckInOpen(DateTime utcNow)
    {
        if (Status is not (EventStatus.Published or EventStatus.Postponed))
            throw new DomainException(
                $"Le contrôle des billets est fermé pour un événement au statut « {Status} ».");

        if (utcNow < StartDate - CheckInOpensBefore)
            throw new DomainException("Le contrôle n'est pas encore ouvert pour cet événement.");

        if (utcNow > EndDate + CheckInClosesAfter)
            throw new DomainException("Le contrôle est terminé pour cet événement.");
    }

    private void EnsureStatus(string action, params EventStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException(
                $"Impossible de {action} un événement au statut « {Status} ».");
    }
}