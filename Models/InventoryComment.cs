using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    /// <summary>
    /// Inventory-level discussion comment (separate from item-level Comments).
    /// </summary>
    public class InventoryComment
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;  // Markdown

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
