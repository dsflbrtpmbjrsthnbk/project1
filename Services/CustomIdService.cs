using System.Text.Json;
using UserManagementApp.Models;
using UserManagementApp.Data;
using Microsoft.EntityFrameworkCore;

namespace UserManagementApp.Services
{
    public interface ICustomIdService
    {
        Task<string> GenerateIdAsync(Inventory inventory);
    }

    public class CustomIdService : ICustomIdService
    {
        private readonly ApplicationDbContext _context;

        public CustomIdService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateIdAsync(Inventory inventory)
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

        private async Task<string> ResolveElement(IdElement element, Inventory inventory)
        {
            return element.Type switch
            {
                IdElementType.FixedText => element.Value ?? "",
                IdElementType.GUID => Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper(),
                IdElementType.RandomNumber => GenerateRandom(element.Format),
                IdElementType.DateTime => DateTime.UtcNow.ToString(element.Format ?? "yyyyMMdd"),
                IdElementType.Sequence => await GetNextSequence(inventory, element.Format),
                _ => ""
            };
        }

        private string GenerateRandom(string? format)
        {
            var random = new Random();
            // Simple implementation of 20-bit, 32-bit etc or based on format D4, X5
            if (format != null && format.StartsWith("X")) 
                return random.Next(1000, 99999).ToString("X");
            return random.Next(1000, 9999).ToString();
        }

        private async Task<string> GetNextSequence(Inventory inventory, string? format)
        {
            var count = await _context.Items.CountAsync(i => i.InventoryId == inventory.Id);
            var next = count + 1;
            return next.ToString(format ?? "D");
        }

        public enum IdElementType { FixedText, RandomNumber, GUID, DateTime, Sequence }
        public class IdElement {
            public IdElementType Type { get; set; }
            public string? Value { get; set; }
            public string? Format { get; set; }
        }
    }
}
