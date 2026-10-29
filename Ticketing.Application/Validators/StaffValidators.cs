using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.DTOs.Staff;

namespace Ticketing.Application.Validators;

public class AssignStaffRequestValidator : AbstractValidator<AssignStaffRequest>
{
    public AssignStaffRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
