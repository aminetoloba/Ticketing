using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Orders;

namespace Ticketing.Application.Common.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetMineAsync(CancellationToken cancellationToken = default);
}
