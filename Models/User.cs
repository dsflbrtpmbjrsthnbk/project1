using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public class User : IdentityUser<Guid>
    {
        // IdentityUser already provides:
        // - Id (Guid)
        // - UserName (will be used for email)
        // - Email
        // - PasswordHash
        // - SecurityStamp
        // - EmailConfirmed

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string Status { get; set; } = "unverified";

        public DateTime RegistrationTime { get; set; }

        public DateTime? LastLoginTime { get; set; }

        public bool IsAdmin { get; set; } = false;

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<InventoryAccess> AccessibleInventories { get; set; } = new List<InventoryAccess>();
        
        public bool IsBlocked() => Status == "blocked";
    }
}
