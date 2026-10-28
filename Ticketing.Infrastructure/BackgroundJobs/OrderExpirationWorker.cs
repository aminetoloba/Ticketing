using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.Infrastructure.BackgroundJobs;

public class OrderExpirationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private const int MaxBatchesPerRun = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExpirationWorker> _logger;

    public OrderExpirationWorker(
        IServiceScopeFactory scopeFactory, ILogger<OrderExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Arrêt normal de l'application
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Un worker vit toute la durée de l'application, mais le DbContext non :
            // on ouvre un scope neuf à chaque passage
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOrderExpirationService>();

            var total = 0;
            for (var i = 0; i < MaxBatchesPerRun; i++)
            {
                var expired = await service.ExpireOverdueOrdersAsync(DateTime.UtcNow, stoppingToken);
                if (expired == 0) break;
                total += expired;
            }

            if (total > 0)
                _logger.LogInformation("{Count} commande(s) expirée(s), stock libéré.", total);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ConcurrencyConflictException)
        {
            // Un achat a touché au même stock : on réessaiera à la minute suivante
            _logger.LogWarning("Conflit de stock pendant l'expiration, nouvelle tentative au prochain passage.");
        }
        catch (Exception ex)
        {
            // Une exception non gérée arrêterait toute l'application
            _logger.LogError(ex, "Erreur pendant l'expiration des commandes.");
        }
    }
}
