using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace UserManagementApp.Models
{
    public class Item
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string CustomId { get; set; } = null!; // The "Killer Feature" ID

        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Fixed Custom Fields (3 of each type as per requirement)
        // String fields
        public string? StringField0 { get; set; }
        public string? StringField1 { get; set; }
        public string? StringField2 { get; set; }

        // Multiline fields
        public string? TextField0 { get; set; }
        public string? TextField1 { get; set; }
        public string? TextField2 { get; set; }

        // Numeric fields
        public double? NumberField0 { get; set; }
        public double? NumberField1 { get; set; }
        public double? NumberField2 { get; set; }

        // Link fields
        public string? LinkField0 { get; set; }
        public string? LinkField1 { get; set; }
        public string? LinkField2 { get; set; }

        // Boolean fields
        public bool? BoolField0 { get; set; }
        public bool? BoolField1 { get; set; }
        public bool? BoolField2 { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        
        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }

    public class Comment
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!; // Supports Markdown

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Like
    {
        public Guid ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
