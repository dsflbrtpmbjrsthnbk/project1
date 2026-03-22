namespace UserManagementApp.Models
{
    /// <summary>
    /// Модель тикета поддержки. Не хранится в базе данных — только генерируется в JSON и загружается в Dropbox.
    /// </summary>
    public class SupportTicket
    {
        /// <summary>Email пользователя, который создал тикет</summary>
        public string ReportedBy { get; set; } = string.Empty;

        /// <summary>Имя пользователя</summary>
        public string ReportedByName { get; set; } = string.Empty;

        /// <summary>Название коллекции (если тикет создан со страницы коллекции)</summary>
        public string? Inventory { get; set; }

        /// <summary>Ссылка на страницу, с которой был создан тикет</summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>Приоритет: High, Average, Low</summary>
        public string Priority { get; set; } = "Average";

        /// <summary>Краткое описание проблемы от пользователя</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Дата и время создания тикета (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Email-адреса администраторов, которые получат уведомление</summary>
        public List<string> AdminEmails { get; set; } = new();
    }
}
