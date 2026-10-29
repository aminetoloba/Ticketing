using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.DTOs.CheckIn;

namespace Ticketing.Application.Validators;

public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        // Les codes générés font 32 caractères hexadécimaux
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9A-Fa-f]{32}$").WithMessage("Le format du code est invalide.");
    }
}
