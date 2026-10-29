using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Infrastructure.Persistance;
using Ticketing.Infrastructure.Persistance.Interceptors;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Ticketing.Infrastructure.Persistance.Repositories;
using Ticketing.Infrastructure.BackgroundJobs;
using Ticketing.Infrastructure.Payments;
using Ticketing.Infrastructure.Tickets;

namespace Ticketing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddHostedService<OrderExpirationWorker>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Mêmes règles que le validateur FluentValidation
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));

        return services;
    }
}
