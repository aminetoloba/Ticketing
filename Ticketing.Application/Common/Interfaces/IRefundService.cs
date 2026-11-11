using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Orders;

namespace Ticketing.Application.Common.Interfaces;

public interface IRefundService
{
    Task<OrderDto> RequestRefundAsync(Guid orderId, CancellationToken cancellationToken = default);
}
