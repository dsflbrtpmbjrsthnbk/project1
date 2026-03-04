using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementApp.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    }

    public class InventoryTag
    {
        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;
        
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
