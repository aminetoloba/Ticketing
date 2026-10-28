using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Orders;

namespace Ticketing.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<OrderDto> PayOrderAsync(
        Guid orderId, PayOrderRequest request, CancellationToken cancellationToken = default);
}
