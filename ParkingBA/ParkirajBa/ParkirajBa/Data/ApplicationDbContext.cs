using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ParkirajBa.Models;

namespace ParkirajBa.Data
{

    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<ParkingObject> ParkingObject { get; set; }
        public DbSet<Pricing> Pricing { get; set; }
        public DbSet<RegisteredUser> RegisteredUser { get; set; }
        public DbSet<Owner> Owner { get; set; }
        public DbSet<Administrator> Administrator { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParkingObject>().ToTable("ParkingObject");
            modelBuilder.Entity<Pricing>().ToTable("Pricing");

            modelBuilder.Entity<RegisteredUser>().ToTable("RegisteredUser");
            modelBuilder.Entity<Owner>().ToTable("Owner");
            modelBuilder.Entity<Administrator>().ToTable("Administrator");

            base.OnModelCreating(modelBuilder);
        }
    }

}

