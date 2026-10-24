using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistance.Configurations;

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("TicketTypes");
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Price).HasPrecision(18, 2);

        // Concurrence optimiste : PostgreSQL utilise sa colonne système xmin.
        // Si deux achats modifient la même ligne en même temps,
        // le second échoue avec DbUpdateConcurrencyException.
        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}
