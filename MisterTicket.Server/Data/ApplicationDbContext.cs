using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Scene> Scene { get; set; }
    public DbSet<PriceZone> PriceZones { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<EventSeat> EventSeats { get; set; }

    // Data/ApplicationDbContext.cs

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Résoudre le conflit de cascade (Cycles ou multiple cascade paths)
        // On désactive la suppression en cascade entre PriceZone et Seat
        modelBuilder.Entity<Seat>()
            .HasOne(s => s.PriceZone)
            .WithMany() // ou WithMany(pz => pz.Seats) si vous ajoutez la liste dans PriceZone
            .HasForeignKey(s => s.PriceZoneId)
            .OnDelete(DeleteBehavior.NoAction); // Changement ici : NoAction au lieu de Cascade

        // 2. Configurer la précision des types decimal (Supprime les avertissements de précision)
        modelBuilder.Entity<PriceZone>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Seat>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Value)
            .HasPrecision(18, 2);

        // 3. Contrainte d'unicité pour EventSeat (rappel)
        modelBuilder.Entity<EventSeat>()
            .HasIndex(es => new { es.EventId, es.SeatId })
            .IsUnique();
    }

}