using Microsoft.EntityFrameworkCore;

namespace Propertease.Models

{
    public class ProperteaseDbContext : DbContext
    {
        public ProperteaseDbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }

        // Configure the model to match the database schema
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: You can add any additional configuration here.
            modelBuilder.Entity<User>(entity =>
            {
                // Make sure the Email is unique
                entity.HasIndex(e => e.Email).IsUnique();

                // Optionally, you can add other configurations like max length for properties
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.ContactNumber).HasMaxLength(15);  // Assuming max length of phone number is 15
            });
        }
    }
}
