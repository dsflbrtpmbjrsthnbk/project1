using Microsoft.AspNetCore.SignalR;
using UserManagementApp.Models;

namespace UserManagementApp.Hubs
{
    public class DiscussionHub : Hub
    {
        public async Task JoinInventory(string inventoryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, inventoryId);
        }

        public async Task SendComment(string inventoryId, string userName, string content)
        {
            // In a real app, save to DB here or in the caller
            await Clients.Group(inventoryId).SendAsync("ReceiveComment", userName, content, DateTime.UtcNow.ToString("g"));
        }
    }
}
