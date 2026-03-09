using System.Text.Json;
using UserManagementApp.Models;
using UserManagementApp.Data;
using Microsoft.EntityFrameworkCore;

namespace UserManagementApp.Services
{
    public interface ICustomIdService
    {
        Task<string> GenerateIdAsync(UserManagementApp.Models.Inventory inventory);
    }

    public class CustomIdService : ICustomIdService
    {
        private readonly ApplicationDbContext _context;

        public CustomIdService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateIdAsync(UserManagementApp.Models.Inventory inventory)
        {
            if (string.IsNullOrEmpty(inventory.CustomIdPattern) || inventory.CustomIdPattern == "[]")
            {
                var count = await _context.Items.CountAsync(i => i.InventoryId == inventory.Id);
                return $"ITEM-{count + 1}";
            }

            var elements = JsonSerializer.Deserialize<List<IdElement>>(inventory.CustomIdPattern);
            if (elements == null) return "ERR-ID";

            var result = "";
            foreach (var el in elements)
            {
                result += await ResolveElement(el, inventory);
            }

            return result;
        }

        private async Task<string> ResolveElement(IdElement element, UserManagementApp.Models.Inventory inventory)
        {
            var random = new Random();
            return element.Type switch
            {
                IdElementType.FixedText => element.Value ?? "",
                IdElementType.GUID => Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper(),
                IdElementType.Random20Bit => (random.Next() & 0xFFFFF).ToString("X5"),
                IdElementType.Random32Bit => ((uint)random.Next()).ToString("X8"),
                IdElementType.Random6Digit => random.Next(100000, 999999).ToString(),
                IdElementType.Random9Digit => random.Next(100000000, 999999999).ToString(),
                IdElementType.DateTime => DateTime.UtcNow.ToString(string.IsNullOrWhiteSpace(element.Format) ? "yyyyMMdd" : element.Format),
                IdElementType.Sequence => await GetNextSequence(inventory, element.Format),
                _ => ""
            };
        }

        private async Task<string> GetNextSequence(UserManagementApp.Models.Inventory inventory, string? format)
        {
            var count = await _context.Items.CountAsync(i => i.InventoryId == inventory.Id);
            var next = count + 1;
            return next.ToString(string.IsNullOrWhiteSpace(format) ? "D3" : format);
        }
    }
}
