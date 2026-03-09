using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IUserService
    {
        Task<User?> GetCurrentUserAsync();
    }
}
