using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modifiedEntries)
            {
                // Only manual update RowVersion if NOT Postgres (Postgres uses xmin)
                if (!Database.IsNpgsql())
                {
                    var rowVersionProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "RowVersion");
                    if (rowVersionProp != null)
                    {
                        rowVersionProp.CurrentValue = Guid.NewGuid().ToByteArray();
                    }
                }

                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public new DbSet<User> Users { get; set; } = null!;
        public DbSet<UserManagementApp.Models.Inventory> Inventories { get; set; } = null!;
        public DbSet<UserManagementApp.Models.Item> Items { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<InventoryTag> InventoryTags { get; set; } = null!;
        public DbSet<FieldDefinition> FieldDefinitions { get; set; } = null!;
        public DbSet<InventoryAccess> InventoryAccesses { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;
        public DbSet<InventoryComment> InventoryComments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration is mostly handled by base.OnModelCreating
            // Custom properties like Status, IsAdmin etc. are mapped automatically if they match column names or via convention.
            // We only need to ensure the gen_random_uuid() for ID if we want that specifically, 
            // but Identity handles ID generation usually.
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<User>()
                    .Property(u => u.Id)
                    .HasDefaultValueSql("gen_random_uuid()");
            }
                
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Books" },
                new Category { Id = 2, Name = "Electronics" },
                new Category { Id = 3, Name = "Collectibles" },
                new Category { Id = 4, Name = "Tools & Hardware" },
                new Category { Id = 5, Name = "Other" }
            );

            // Inventory configuration
            modelBuilder.Entity<UserManagementApp.Models.Inventory>()
                .HasOne(i => i.Owner)
                .WithMany(u => u.Inventories)
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserManagementApp.Models.Inventory>()
                .Property(i => i.CategoryId)
                .HasColumnName("Category");

            // InventoryAccess configuration
            modelBuilder.Entity<InventoryAccess>()
                .HasKey(ia => new { ia.InventoryId, ia.UserId });
            modelBuilder.Entity<InventoryAccess>()
                .HasOne(ia => ia.Inventory)
                .WithMany(i => i.AllowedUsers)
                .HasForeignKey(ia => ia.InventoryId);
            modelBuilder.Entity<InventoryAccess>()
                .HasOne(ia => ia.User)
                .WithMany(u => u.AccessibleInventories)
                .HasForeignKey(ia => ia.UserId);

            // InventoryTag configuration
            modelBuilder.Entity<InventoryTag>()
                .HasKey(it => new { it.InventoryId, it.TagId });

            // Item configuration
            modelBuilder.Entity<UserManagementApp.Models.Item>()
                .HasIndex(i => new { i.InventoryId, i.CustomId })
                .IsUnique(); // Killer Feature requirement: uniqueness per inventory

            // Like configuration
            modelBuilder.Entity<Like>()
                .HasKey(l => new { l.ItemId, l.UserId });

            // Concurrency configuration
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<UserManagementApp.Models.Inventory>()
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .IsRowVersion();

                modelBuilder.Entity<UserManagementApp.Models.Item>()
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .IsRowVersion();
            }
            else
            {
                modelBuilder.Entity<UserManagementApp.Models.Inventory>()
                    .Property(i => i.RowVersion)
                    .IsRowVersion();
                modelBuilder.Entity<UserManagementApp.Models.Item>()
                    .Property(i => i.RowVersion)
                    .IsRowVersion();
            }

            // Full-Text Search Configuration (PostgreSQL)
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<UserManagementApp.Models.Inventory>()
                    .HasGeneratedTsVectorColumn(
                        i => i.SearchVector,
                        "english", 
                        i => new { i.Title, i.Description }
                    )
                    .HasIndex(i => i.SearchVector)
                    .HasMethod("GIN");

                modelBuilder.Entity<UserManagementApp.Models.Item>()
                    .HasGeneratedTsVectorColumn(
                        i => i.SearchVector,
                        "english",
                        i => new { i.CustomId, i.StringField0, i.StringField1, i.StringField2, i.TextField0, i.TextField1, i.TextField2 }
                    )
                    .HasIndex(i => i.SearchVector)
                    .HasMethod("GIN");
            }
        }
    }
}
