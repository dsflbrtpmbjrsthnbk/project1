using System.Threading.Tasks;

namespace UserManagementApp.Services
{
    public interface ISalesforceService
    {
        Task<string> GetAccessTokenAsync();
        Task<string> CreateAccountAsync(string name, string? phone = null, string? website = null);
        Task<string> CreateContactAsync(string accountId, string firstName, string lastName, string email, string? title = null, string? department = null);
    }
}
