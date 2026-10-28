using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.DTOs.Orders;

namespace Ticketing.Application.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public const int MaxTicketsPerOrder = 10;

    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();

        RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La commande doit contenir au moins une ligne.")
            .Must(lines => lines.Select(l => l.TicketTypeId).Distinct().Count() == lines.Count)
            .WithMessage("Un type de billet ne peut apparaître qu'une seule fois.")
            .Must(lines => lines.Sum(l => l.Quantity) <= MaxTicketsPerOrder)
            .WithMessage($"Une commande ne peut pas dépasser {MaxTicketsPerOrder} billets.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.TicketTypeId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, MaxTicketsPerOrder);
        });
    }
}

public class PayOrderRequestValidator : AbstractValidator<PayOrderRequest>
{
    public PayOrderRequestValidator()
    {
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(100);
    }
}
