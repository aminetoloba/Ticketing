using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs.Users;

namespace Ticketing.Application.Validators;

public class UserQueryValidator : AbstractValidator<UserQuery>
{
    public UserQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);

        RuleFor(x => x.Role)
            .Must(role => Roles.All.Contains(role!))
            .When(x => !string.IsNullOrEmpty(x.Role))
            .WithMessage("Rôle inconnu.");
    }
}

public class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Roles.All.Contains(role))
            .WithMessage("Rôle inconnu.");
    }
}
