using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Orders;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<PayOrderRequest> _payValidator;

    public OrdersController(
        IOrderService orders,
        IPaymentService payments,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<PayOrderRequest> payValidator)
    {
        _orders = orders;
        _payments = payments;
        _createValidator = createValidator;
        _payValidator = payValidator;
    }

    [HttpPost]
    [Authorize(Roles = Policies.Buyers)]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _orders.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{id:guid}/pay")]
    [Authorize(Roles = Policies.Buyers)]
    public async Task<ActionResult<OrderDto>> Pay(
    Guid id, PayOrderRequest request, CancellationToken cancellationToken)
    {
        await _payValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _payments.PayOrderAsync(id, request, cancellationToken));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await _orders.GetMineAsync(cancellationToken));
}