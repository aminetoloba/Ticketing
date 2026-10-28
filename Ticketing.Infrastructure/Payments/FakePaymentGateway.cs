using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.Infrastructure.Payments;

public class FakePaymentGateway : IPaymentGateway
{
    // Jeton de test sur le modèle de Stripe : aucune donnée de carte n'est jamais traitée
    public const string DeclinedToken = "tok_chargeDeclined";

    public Task<PaymentResult> ChargeAsync(
        Guid orderId, decimal amount, string paymentMethod, CancellationToken cancellationToken = default)
    {
        if (paymentMethod == DeclinedToken)
            return Task.FromResult(new PaymentResult(false, null, "Carte refusée."));

        return Task.FromResult(new PaymentResult(true, $"fake_pi_{Guid.NewGuid():N}", null));
    }

    public Task<RefundResult> RefundAsync(
        string providerReference, decimal amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RefundResult(true, null));
}
