using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Models;
using ParkirajBA.Models;

namespace ParkirajBa.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<RegisteredUser>(options)
    {
        public DbSet<ParkingObject> ParkingObject { get; set; }
        public DbSet<Pricing> Pricing { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParkingObject>().ToTable("ParkingObject");
            modelBuilder.Entity<Pricing>().ToTable("Pricing");
            modelBuilder.Entity<Owner>().ToTable("Owner");
            modelBuilder.Entity<Administrator>().ToTable("Administrator");

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}