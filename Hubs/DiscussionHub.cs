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

        /// <summary>
        /// Called by client to post a comment. Saves to DB, then broadcasts to all viewers.
        /// </summary>
        public async Task SendComment(string inventoryId, string userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            if (!Guid.TryParse(inventoryId, out var invId) ||
                !Guid.TryParse(userId, out var uid))
                return;

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return;

            // Find a valid item ID – discussion is linked to inventory, not a specific item.
            // We use a sentinel approach: store inventory comments in a separate table.
            // Here we persist the comment to the Comments table associated with a special sentinel item.
            // SIMPLER: Persist directly as a DB record (inventory-level discussion).
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

            // Broadcast to all connected clients in the group
            await Clients.Group(inventoryId).SendAsync(
                "ReceiveComment",
                user.Id.ToString(),
                user.Name,
                content.Trim(),
                comment.CreatedAt.ToString("g"));
        }
    }
}
