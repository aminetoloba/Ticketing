using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Exceptions;
using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketType : AuditableEntity
{
    private TicketType() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int TotalQuantity { get; private set; }
    public int SoldQuantity { get; private set; }

    // Jeton de concurrence optimiste, géré par la base de données
    public uint RowVersion { get; private set; }

    public int Remaining => TotalQuantity - SoldQuantity;

    public static TicketType Create(Guid eventId, string name, decimal price, int totalQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Le nom du type de billet est obligatoire.");
        if (price < 0)
            throw new DomainException("Le prix ne peut pas être négatif.");
        if (totalQuantity <= 0)
            throw new DomainException("La quantité totale doit être supérieure à zéro.");

        return new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name.Trim(),
            Price = price,
            TotalQuantity = totalQuantity
        };
    }

    // Réserve des places (création d'une commande)
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("La quantité doit être supérieure à zéro.");
        if (quantity > Remaining)
            throw new DomainException("Plus assez de places disponibles.");

        SoldQuantity += quantity;
    }

    // Libère des places (commande expirée ou annulée)
    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("La quantité doit être supérieure à zéro.");
        if (quantity > SoldQuantity)
            throw new DomainException("Impossible de libérer plus de places que réservées.");

        SoldQuantity -= quantity;
    }
}
