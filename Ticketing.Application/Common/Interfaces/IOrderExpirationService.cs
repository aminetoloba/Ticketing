using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common.Interfaces;

public interface IOrderExpirationService
{
    // Retourne le nombre de commandes expirées
    Task<int> ExpireOverdueOrdersAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
