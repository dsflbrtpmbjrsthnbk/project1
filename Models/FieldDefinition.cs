using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public enum FieldType
    {
        String,  
        Multiline, 
        Numeric,  
        Link,    
        Boolean   
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

        public string? Description { get; set; } 

        public FieldType Type { get; set; }

        public int SlotIndex { get; set; } 

        public bool IsInTableView { get; set; } = true;

        public int DisplayOrder { get; set; }
    }
}
