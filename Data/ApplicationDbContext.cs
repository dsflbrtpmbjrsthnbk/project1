using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Inventory> Inventories { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<InventoryTag> InventoryTags { get; set; } = null!;
        public DbSet<FieldDefinition> FieldDefinitions { get; set; } = null!;
        public DbSet<InventoryAccess> InventoryAccesses { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Inventory configuration
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Owner)
                .WithMany(u => u.Inventories)
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

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
            modelBuilder.Entity<Item>()
                .HasIndex(i => new { i.InventoryId, i.CustomId })
                .IsUnique(); // Killer Feature requirement: uniqueness per inventory

            // Like configuration
            modelBuilder.Entity<Like>()
                .HasKey(l => new { l.ItemId, l.UserId });

            // Full-Text Search Configuration (PostgreSQL)
            // We'll create a search vector for Inventories and Items
            modelBuilder.Entity<Inventory>()
                .HasGeneratedTsVectorColumn(
                    i => i.SearchVector,
                    "english", 
                    i => new { i.Title, i.Description }
                )
                .HasIndex(i => i.SearchVector)
                .HasMethod("GIN");

            modelBuilder.Entity<Item>()
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
