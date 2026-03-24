using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IDropboxService
    {
        Task<bool> UploadSupportTicketAsync(SupportTicket ticket);
    }
}
