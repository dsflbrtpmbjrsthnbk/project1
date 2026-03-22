using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IDropboxService
    {
        /// <summary>
        /// Загружает тикет поддержки как JSON файл в Dropbox.
        /// Возвращает true если загрузка успешна, false если произошла ошибка.
        /// </summary>
        Task<bool> UploadSupportTicketAsync(SupportTicket ticket);
    }
}
