using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Identity;

namespace Ticketing.Infrastructure.Persistance.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Location).IsRequired().HasMaxLength(300);
        builder.Property(e => e.PostponementReason).HasMaxLength(1000);

        // Le statut est stocké en texte (« Published ») : plus lisible en base
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Un événement appartient à un organisateur (utilisateur Identity)
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un événement possède plusieurs types de billets
        builder.HasMany(e => e.TicketTypes)
            .WithOne()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF remplit la liste privée _ticketTypes directement
        builder.Navigation(e => e.TicketTypes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.OrganizerId);
        builder.HasIndex(e => e.Status);
    }
}