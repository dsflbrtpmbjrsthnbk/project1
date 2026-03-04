using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public enum FieldType
    {
        String,   // Text field
        Multiline, // Multi-line text (Markdown)
        Numeric,  // Number
        Link,     // Document/Image link
        Boolean   // True/False (Checkbox)
    }

    public class FieldDefinition
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; } // Tooltip/Hint

        public FieldType Type { get; set; }

        // Up to 3 of each type. We'll map these to specific columns in the Item table.
        // This index (0, 1, 2) tells us WHICH column of that type to use.
        public int SlotIndex { get; set; } 

        public bool IsInTableView { get; set; } = true;

        public int DisplayOrder { get; set; }
    }
}
