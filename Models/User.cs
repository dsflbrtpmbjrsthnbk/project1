using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(50)]
        public string Status { get; set; } = "unverified";

        public DateTime RegistrationTime { get; set; }

        public string? EmailVerificationToken { get; set; }

        public DateTime? LastLoginTime { get; set; }

        public bool IsAdmin { get; set; } = false;

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<InventoryAccess> AccessibleInventories { get; set; } = new List<InventoryAccess>();
        
        public bool IsBlocked() => Status == "blocked";
    }
}
