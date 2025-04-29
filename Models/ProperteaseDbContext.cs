using Microsoft.EntityFrameworkCore;
using PROPERTEASE.Models;

namespace Propertease.Models

{
    public class ProperteaseDbContext : DbContext
    {
        public ProperteaseDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Properties> properties { get; set; }
        public DbSet<House> Houses { get; set; }

        // DbSet for Lands table
        public DbSet<Land> Lands { get; set; }
        public DbSet<BoostedProperty> BoostedProperties { get; set; }
        // DbSet for Apartments table
        // In your ApplicationDbContext class
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        //public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<BuyerRating> BuyerRatings { get; set; }
        public DbSet<SellerRating> SellerRatings { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        //public DbSet<Payment> Payments { get; set; }
        // ProperteaseDbContext.cs
        public DbSet<PropertyViewingRequest> PropertyViewingRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<House>().HasOne(h => h.Properties).WithMany(p => p.Houses).HasForeignKey(h => h.PropertyID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Land>().HasOne(l => l.Properties).WithMany(p => p.Lands).HasForeignKey(l => l.PropertyID).OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Apartment>().HasOne(a => a.Properties).WithMany(p => p.Apartments).HasForeignKey(a => a.PropertyID).OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<BoostedProperty>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaymentStatus).HasDefaultValue("Pending");
                entity.HasOne(e => e.Property)
                    .WithMany()
                    .HasForeignKey(e => e.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PropertyViewingRequest>()
          .HasOne(pvr => pvr.Buyer)
          .WithMany(u => u.PropertyViewingRequests)
          .HasForeignKey(pvr => pvr.BuyerId)
          .OnDelete(DeleteBehavior.Cascade); // This is fine

            modelBuilder.Entity<PropertyViewingRequest>()
                .HasOne(pvr => pvr.Properties)
                .WithMany(p => p.PropertyViewingRequests)
                .HasForeignKey(pvr => pvr.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>(entity =>
            {
                // Define the relationship with User (Recipient)
                entity.HasOne(n => n.Recipient)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.RecipientId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Define the relationship with Property (optional relationship)
                entity.HasOne(n => n.RelatedProperty)
                      .WithMany(p => p.Notifications)
                      .HasForeignKey(n => n.RelatedPropertyId)
                      .IsRequired(false) // Make it optional
                      .OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<SellerRating>()
               .HasOne(sr => sr.Buyer)
               .WithMany()
               .HasForeignKey(sr => sr.BuyerId)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Seller)
                .WithMany()
                .HasForeignKey(sr => sr.SellerId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PropertyView>()
              .HasOne(pv => pv.Property)
              .WithMany()
              .HasForeignKey(pv => pv.PropertyId)
              .OnDelete(DeleteBehavior.Restrict); // Change to Restrict

            modelBuilder.Entity<PropertyView>()
                .HasOne(pv => pv.User)
                .WithMany()
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Property)
                .WithMany()
                .HasForeignKey(sr => sr.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ForumPost>()
                        .HasOne(fp => fp.User)
                        .WithMany(u => u.ForumPosts)
                        .HasForeignKey(fp => fp.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BuyerRating>()
                .HasOne(ur => ur.Buyer)
                .WithMany()
                .HasForeignKey(ur => ur.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BuyerRating>()
                .HasOne(ur => ur.Seller)
                .WithMany()
                .HasForeignKey(ur => ur.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BuyerRating>()
                .HasOne(ur => ur.Property)
                .WithMany()
                .HasForeignKey(ur => ur.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PropertyImage>()
                    .HasOne(pi => pi.Property)
                    .WithMany(p => p.PropertyImages)
                    .HasForeignKey(pi => pi.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
            // Configure UserRating -> User (RaterUser) relationship
       

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

            modelBuilder.Entity<Favorite>()
     .HasOne(f => f.Property)
     .WithMany(p => p.Favorites)
     .HasForeignKey(f => f.PropertyId)
     .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Favorite>()
    .HasOne(f => f.User)
    .WithMany(u => u.Favorites)
    .HasForeignKey(f => f.UserId)
    .OnDelete(DeleteBehavior.NoAction);

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
