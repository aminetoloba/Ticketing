using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Validators;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.Services;

namespace Ticketing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Enregistre tous les validateurs de l'assembly d'un coup
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
