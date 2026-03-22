using System.Text;
using System.Text.Json;
using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public class DropboxService : IDropboxService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DropboxService> _logger;
        private readonly HttpClient _httpClient;

        // URL для загрузки файлов в Dropbox через их Content API
        private const string DropboxUploadUrl = "https://content.dropboxapi.com/2/files/upload";

        public DropboxService(IConfiguration configuration, ILogger<DropboxService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<bool> UploadSupportTicketAsync(SupportTicket ticket)
        {
            try
            {
                // Берем токен из настроек (appsettings.json или Environment Variables)
                var accessToken = _configuration["Dropbox:AccessToken"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Dropbox access token is not configured.");
                    return false;
                }

                // Генерируем уникальное имя файла: ticket_userId_timestamp.json
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                var safeUser = ticket.ReportedBy.Replace("@", "_at_").Replace(".", "_");
                var fileName = $"ticket_{safeUser}_{timestamp}.json";
                var dropboxPath = $"/tickets/{fileName}";

                // Сериализуем тикет в красивый JSON (с отступами для читаемости)
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var jsonContent = JsonSerializer.Serialize(ticket, jsonOptions);
                var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

                // Создаем HTTP запрос к Dropbox API
                var request = new HttpRequestMessage(HttpMethod.Post, DropboxUploadUrl);

                // Заголовок авторизации (Bearer токен)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Заголовок Dropbox-API-Arg — параметры загрузки файла в JSON формате
                var apiArg = new
                {
                    path = dropboxPath,
                    mode = "add",           // Добавить файл (не перезаписывать)
                    autorename = true,      // Если файл уже есть — переименовать автоматически
                    mute = false            // Уведомить Power Automate о новом файле
                };
                request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(apiArg));

                // Тело запроса — сами байты JSON файла
                request.Content = new ByteArrayContent(jsonBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Отправляем запрос
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Support ticket uploaded to Dropbox: {FileName}", fileName);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Dropbox upload failed: {StatusCode} — {Error}", response.StatusCode, errorBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while uploading support ticket to Dropbox.");
                return false;
            }
        }
    }
}
