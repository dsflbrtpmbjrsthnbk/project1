using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers
{
    /// <summary>
    /// Public REST API for external integrations (Odoo, etc.).
    /// Every endpoint requires the X-Api-Token header that matches
    /// the token stored on the requested Inventory.
    /// </summary>
    [ApiController]
    [Route("api/inventory")]
    public class InventoryApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public InventoryApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────────────────────────────────
        // GET /api/inventory/{id}
        // Header: X-Api-Token: <token>
        // Returns aggregated statistics for the inventory.
        // ──────────────────────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetInventory(Guid id)
        {
            var token = Request.Headers["X-Api-Token"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { error = "X-Api-Token header is missing." });

            var inventory = await _db.Inventories
                .Include(i => i.FieldDefinitions.OrderBy(f => f.DisplayOrder))
                .Include(i => i.Items)
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
                return NotFound(new { error = "Inventory not found." });

            if (inventory.ApiToken == null || inventory.ApiToken != token)
                return StatusCode(403, new { error = "Invalid API token." });

            var items = inventory.Items.ToList();
            var fields = inventory.FieldDefinitions.ToList();

            var fieldStats = fields.Select(field => BuildFieldStats(field, items)).ToList();

            var result = new
            {
                id            = inventory.Id,
                title         = inventory.Title,
                description   = inventory.Description,
                category      = inventory.Category?.Name,
                owner         = inventory.Owner?.Name,
                isPublic      = inventory.IsPublic,
                createdAt     = inventory.CreatedAt,
                updatedAt     = inventory.UpdatedAt,
                itemCount     = items.Count,
                fields        = fieldStats
            };

            return Ok(result);
        }

        // ──────────────────────────────────────────────────────────────────────
        // POST /api/inventory/{id}/token
        // Header: (none — requires the inventory owner to be logged in via
        //          the Blazor UI; this endpoint is called internally)
        // Generates (or regenerates) the API token for an inventory.
        // Protected by requiring the owner's session cookie.
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("{id:guid}/token")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GenerateToken(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inventory == null)
                return NotFound();

            var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            // Only the owner or an admin can generate tokens
            if (inventory.OwnerId != userId && !(dbUser?.IsAdmin ?? false))
                return Forbid();

            inventory.ApiToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();
            inventory.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { token = inventory.ApiToken });
        }

        // ══════════════════════════════════════════════════════════════════════
        // Aggregation helpers
        // ══════════════════════════════════════════════════════════════════════

        private static object BuildFieldStats(FieldDefinition field, List<Item> items)
        {
            object stats = field.Type switch
            {
                FieldType.Numeric  => BuildNumericStats(field, items),
                FieldType.Boolean  => BuildBoolStats(field, items),
                FieldType.String   => BuildTextStats(field, items, isMultiline: false),
                FieldType.Multiline => BuildTextStats(field, items, isMultiline: true),
                FieldType.Link     => BuildLinkStats(field, items),
                _                  => new { }
            };

            return new
            {
                id    = field.Id,
                title = field.Title,
                type  = field.Type.ToString(),
                stats
            };
        }

        private static object BuildNumericStats(FieldDefinition f, List<Item> items)
        {
            var values = items
                .Select(i => GetNumericValue(i, f.SlotIndex))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (!values.Any())
                return new { count = 0, min = (double?)null, max = (double?)null, avg = (double?)null };

            return new
            {
                count = values.Count,
                min   = values.Min(),
                max   = values.Max(),
                avg   = Math.Round(values.Average(), 2)
            };
        }

        private static object BuildBoolStats(FieldDefinition f, List<Item> items)
        {
            var values = items
                .Select(i => GetBoolValue(i, f.SlotIndex))
                .Where(v => v.HasValue)
                .ToList();

            return new
            {
                count      = values.Count,
                trueCount  = values.Count(v => v == true),
                falseCount = values.Count(v => v == false)
            };
        }

        private static object BuildTextStats(FieldDefinition f, List<Item> items, bool isMultiline)
        {
            var values = items
                .Select(i => isMultiline ? GetTextValue(i, f.SlotIndex) : GetStringValue(i, f.SlotIndex))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var topValues = values
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new { value = g.Key, count = g.Count() })
                .ToList();

            return new { totalNonEmpty = values.Count, topValues };
        }

        private static object BuildLinkStats(FieldDefinition f, List<Item> items)
        {
            var nonNull = items.Count(i => !string.IsNullOrWhiteSpace(GetLinkValue(i, f.SlotIndex)));
            return new { nonNullCount = nonNull, totalItems = items.Count };
        }

        // ── Column accessors ──────────────────────────────────────────────────

        private static double? GetNumericValue(Item item, int slot) => slot switch
        {
            0 => item.NumberField0,
            1 => item.NumberField1,
            2 => item.NumberField2,
            _ => null
        };

        private static bool? GetBoolValue(Item item, int slot) => slot switch
        {
            0 => item.BoolField0,
            1 => item.BoolField1,
            2 => item.BoolField2,
            _ => null
        };

        private static string? GetStringValue(Item item, int slot) => slot switch
        {
            0 => item.StringField0,
            1 => item.StringField1,
            2 => item.StringField2,
            _ => null
        };

        private static string? GetTextValue(Item item, int slot) => slot switch
        {
            0 => item.TextField0,
            1 => item.TextField1,
            2 => item.TextField2,
            _ => null
        };

        private static string? GetLinkValue(Item item, int slot) => slot switch
        {
            0 => item.LinkField0,
            1 => item.LinkField1,
            2 => item.LinkField2,
            _ => null
        };
    }
}
