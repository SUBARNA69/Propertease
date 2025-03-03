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
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<PropertyComment> PropertyComments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<House>().HasOne(h => h.Properties).WithMany(p => p.Houses).HasForeignKey(h => h.PropertyID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Land>().HasOne(l => l.Properties).WithMany(p => p.Lands).HasForeignKey(l => l.PropertyID).OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Apartment>().HasOne(a => a.Properties).WithMany(p => p.Apartments).HasForeignKey(a => a.PropertyID).OnDelete(DeleteBehavior.Cascade);
           
            modelBuilder.Entity<ForumPost>()
                        .HasOne(fp => fp.User)
                        .WithMany(u => u.ForumPosts)
                        .HasForeignKey(fp => fp.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRating>()
                           .HasOne(ur => ur.RatedUser)
                           .WithMany(u => u.RatingsReceived)
                           .HasForeignKey(ur => ur.RatedUserId)
                           .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PropertyImage>()
    .HasOne(pi => pi.Property)
    .WithMany(p => p.PropertyImages)
    .HasForeignKey(pi => pi.PropertyId)
    .OnDelete(DeleteBehavior.Cascade);
            // Configure UserRating -> User (RaterUser) relationship
            modelBuilder.Entity<UserRating>()
                        .HasOne(ur => ur.RaterUser)
                        .WithMany(u => u.RatingsGiven)
                        .HasForeignKey(ur => ur.RaterUserId)
                        .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PropertyComment>()
                        .HasOne(pc => pc.Property)
                        .WithMany(p => p.PropertyComments)
                        .HasForeignKey(pc => pc.PropertyId)
                        .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PropertyComment>()
                        .HasOne(pc => pc.User)
                        .WithMany(u => u.PropertyComments)
                        .HasForeignKey(pc => pc.UserId)
                        .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ForumComment>()
                    .HasOne(fc => fc.User)
                    .WithMany(u => u.ForumComments)
                    .HasForeignKey(fc => fc.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Disable cascade delete for ForumComment -> User

            // Configure ForumComment -> ForumPost relationship
            modelBuilder.Entity<ForumComment>()
                .HasOne(fc => fc.ForumPost)
                .WithMany(fp => fp.Comments)
                .HasForeignKey(fc => fc.ForumPostId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete for ForumComment -> ForumPost
           
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
