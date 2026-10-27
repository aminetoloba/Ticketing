using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.DTOs.Events;

namespace Ticketing.Application.Validators;

public class SaveEventRequestValidator : AbstractValidator<SaveEventRequest>
{
    public SaveEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("La date de fin doit être après la date de début.");
    }
}

public class EventQueryValidator : AbstractValidator<EventQuery>
{
    public EventQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(300);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("La date de fin du filtre doit être après la date de début.");
    }
}
