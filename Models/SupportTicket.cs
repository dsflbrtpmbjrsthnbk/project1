namespace UserManagementApp.Models
{
   
    public class SupportTicket
    {
        ///Email
        public string ReportedBy { get; set; } = string.Empty;

        /// Имя пользователя
        public string ReportedByName { get; set; } = string.Empty;

        /// Название коллекции
        public string? Inventory { get; set; }

        ///Ссылка на страницу
        public string Link { get; set; } = string.Empty;

        ///Приоритет
        public string Priority { get; set; } = "Average";

        ///Краткое описание проблемы 
        public string Summary { get; set; } = string.Empty;

        ///Дата и время 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        ///Email-адреса администраторов
        public List<string> AdminEmails { get; set; } = new();
    }
}
