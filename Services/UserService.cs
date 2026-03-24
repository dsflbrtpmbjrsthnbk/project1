using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public class UserService : IUserService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserService(AuthenticationStateProvider authStateProvider, IServiceScopeFactory scopeFactory)
        {
            _authStateProvider = authStateProvider;
            _scopeFactory = scopeFactory;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userPrincipal = authState.User;

            if (!userPrincipal.Identity?.IsAuthenticated ?? true)
                return null;

            var userIdString = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return null;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
