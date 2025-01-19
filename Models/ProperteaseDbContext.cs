using Microsoft.EntityFrameworkCore;

namespace Propertease.Models

{
    public class ProperteaseDbContext : DbContext
    {
        public ProperteaseDbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Properties> properties { get; set; }
        public DbSet<DashboardViewModel> dashboardViewModels { get; set; }
        public DbSet<House> Houses { get; set; }

        // DbSet for Lands table
        public DbSet<Land> Lands { get; set; }

        // DbSet for Apartments table
        public DbSet<Apartment> Apartments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<House>().HasOne(h => h.Properties).WithMany(p => p.Houses).HasForeignKey(h => h.PropertyID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Land>().HasOne(l => l.Properties).WithMany(p => p.Lands).HasForeignKey(l => l.PropertyID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Apartment>().HasOne(a => a.Properties).WithMany(p => p.Apartments).HasForeignKey(a => a.PropertyID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>(entity =>
            {
                // Make sure the Email is unique
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.ContactNumber).HasMaxLength(15);  
            });
        }
    }
}
