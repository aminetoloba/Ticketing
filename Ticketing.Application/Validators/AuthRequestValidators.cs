using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs.Auth;

namespace Ticketing.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.")
            .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir une majuscule.")
            .Matches("[a-z]").WithMessage("Le mot de passe doit contenir une minuscule.")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir un chiffre.");

        // Jamais Admin ni Staff à l'inscription (voir la note ci-dessous)
        RuleFor(x => x.Role)
            .Must(role => role is Roles.Participant or Roles.Organizer)
            .WithMessage("Le rôle doit être Participant ou Organizer.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}