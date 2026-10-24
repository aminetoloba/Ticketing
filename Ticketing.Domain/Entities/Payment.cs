using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Common;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public class Payment : AuditableEntity
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    // Référence donnée par le fournisseur (Stripe, passerelle simulée)
    public string? ProviderReference { get; private set; }

    public static Payment Create(Guid orderId, decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Le montant du paiement doit être supérieur à zéro.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };
    }

    public void MarkAsSucceeded(string providerReference)
    {
        EnsurePending("valider");
        ProviderReference = providerReference;
        Status = PaymentStatus.Succeeded;
    }

    public void MarkAsFailed()
    {
        EnsurePending("rejeter");
        Status = PaymentStatus.Failed;
    }

    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new DomainException(
                $"Impossible de rembourser un paiement au statut « {Status} ».");

        Status = PaymentStatus.Refunded;
    }

    private void EnsurePending(string action)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(
                $"Impossible de {action} un paiement au statut « {Status} ».");
    }
}
