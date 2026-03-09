using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace UserManagementApp.Models
{
    public class Inventory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; } // Supports Markdown

        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ConcurrencyCheck]
        public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<FieldDefinition> FieldDefinitions { get; set; } = new List<FieldDefinition>();
        public ICollection<InventoryAccess> AllowedUsers { get; set; } = new List<InventoryAccess>();
        
        // Custom ID Format Configuration (JSON serialized string or separate table)
        // For drag-and-drop builder, we'll store the sequence of elements.
        public string CustomIdPattern { get; set; } = "[]"; 
        
        [NotMapped]
        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }

    public class InventoryAccess
    {
        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
