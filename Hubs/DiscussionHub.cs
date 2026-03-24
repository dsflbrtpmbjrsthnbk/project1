using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Hubs
{
    public class DiscussionHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public DiscussionHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinInventory(string inventoryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, inventoryId);
        }

        public async Task SendComment(string inventoryId, string userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            if (!Guid.TryParse(inventoryId, out var invId) ||
                !Guid.TryParse(userId, out var uid))
                return;

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return;

            var comment = new InventoryComment
            {
                Id = Guid.NewGuid(),
                InventoryId = invId,
                UserId = uid,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryComments.Add(comment);
            await _context.SaveChangesAsync();

            await Clients.Group(inventoryId).SendAsync(
                "ReceiveComment",
                user.Id.ToString(),
                user.Name,
                content.Trim(),
                comment.CreatedAt.ToString("g"));
        }
    }
}
