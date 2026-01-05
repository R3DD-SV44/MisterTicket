using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Scene> Scenes { get; set; }
    public DbSet<PriceZone> PriceZones { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<EventSeat> EventSeats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Seat>()
            .HasOne(s => s.PriceZone)
            .WithMany()
            .HasForeignKey(s => s.PriceZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Seat>()
            .HasOne<Scene>()
            .WithMany(sc => sc.Seats)
            .HasForeignKey(s => s.SceneId)
            .OnDelete(DeleteBehavior.NoAction); 

        modelBuilder.Entity<PriceZone>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Seat>().Property(s => s.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.Value).HasPrecision(18, 2);
        modelBuilder.Entity<EventSeat>()
         .HasOne<Seat>()
         .WithMany()
         .HasForeignKey(es => es.SeatId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}