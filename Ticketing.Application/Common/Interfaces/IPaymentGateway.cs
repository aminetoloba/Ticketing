using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common.Interfaces;

public record PaymentResult(bool Succeeded, string? ProviderReference, string? FailureReason);

public record RefundResult(bool Succeeded, string? FailureReason);

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        Guid orderId, decimal amount, string paymentMethod, CancellationToken cancellationToken = default);

    Task<RefundResult> RefundAsync(
        string providerReference, decimal amount, CancellationToken cancellationToken = default);
}
