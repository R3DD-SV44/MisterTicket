using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Scene> Scenes { get; set; } // Note : Tu as utilisé le singulier ici
    public DbSet<PriceZone> PriceZones { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<EventSeat> EventSeats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Scene -> PriceZone -> Seat (Le chemin de cascade que vous voulez)
        modelBuilder.Entity<Seat>()
            .HasOne(s => s.PriceZone)
            .WithMany()
            .HasForeignKey(s => s.PriceZoneId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade autorisé ici

        // 2. Scene -> Seat (Lien direct)
        // On met NoAction ici pour éviter l'erreur 1785. 
        // Les sièges seront quand même supprimés via le chemin n°1.
        modelBuilder.Entity<Seat>()
            .HasOne<Scene>()
            .WithMany(sc => sc.Seats)
            .HasForeignKey(s => s.SceneId)
            .OnDelete(DeleteBehavior.NoAction); // Obligatoire pour SQL Server

        // 3. Précision pour les prix
        modelBuilder.Entity<PriceZone>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Seat>().Property(s => s.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.Value).HasPrecision(18, 2);

        // 4. Unicité EventSeat
        modelBuilder.Entity<EventSeat>()
         .HasOne<Seat>()
         .WithMany()
         .HasForeignKey(es => es.SeatId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}