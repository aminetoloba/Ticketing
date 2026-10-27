using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.DTOs.Events;

namespace Ticketing.Application.Validators;

public class SaveTicketTypeRequestValidator : AbstractValidator<SaveTicketTypeRequest>
{
    public SaveTicketTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Le prix doit être supérieur à zéro.")
            .LessThanOrEqualTo(1_000_000m)
            .PrecisionScale(18, 2, true).WithMessage("Le prix accepte au plus 2 décimales.");

        RuleFor(x => x.TotalQuantity).InclusiveBetween(1, 100_000);
    }
}
