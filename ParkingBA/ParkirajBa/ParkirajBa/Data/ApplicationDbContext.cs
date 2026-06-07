using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Models;

namespace ParkirajBa.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<ParkingObject> ParkingObject { get; set; }
        public DbSet<Pricing> Pricing { get; set; }
        public DbSet<OwnerProfile> Owners { get; set; }
        public DbSet<AdminProfile> Administrators { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<ParkingImage> ParkingImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParkingObject>().ToTable("ParkingObject");
            modelBuilder.Entity<Pricing>().ToTable("Pricing");
            modelBuilder.Entity<OwnerProfile>().ToTable("Owner");
            modelBuilder.Entity<AdminProfile>().ToTable("Administrator");
            modelBuilder.Entity<ParkingImage>().ToTable("ParkingImage");

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Pricing>()
                .HasOne(p => p.ParkingObject)
                .WithMany(o => o.Pricings)
                .HasForeignKey(p => p.ParkingObjectID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}