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

        private const string DropboxUploadUrl = "https://content.dropboxapi.com/2/files/upload";

        public DropboxService(IConfiguration configuration, ILogger<DropboxService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<bool> UploadSupportTicketAsync(SupportTicket ticket)
        {
            var accessToken = _configuration["Dropbox:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Dropbox access token is not configured.");
                throw new Exception("Dropbox access token is missing in appsettings.json or environment variables.");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var safeUser = ticket.ReportedBy.Replace("@", "_at_").Replace(".", "_");
            var fileName = $"ticket_{safeUser}_{timestamp}.json";
            var dropboxPath = $"/tickets/{fileName}";

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var jsonContent = JsonSerializer.Serialize(ticket, jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

            using var request = new HttpRequestMessage(HttpMethod.Post, DropboxUploadUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var apiArg = new
            {
                path = dropboxPath,
                mode = "add",
                autorename = true,
                mute = false
            };
            request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(apiArg));

            request.Content = new ByteArrayContent(jsonBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

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
                throw new Exception($"Dropbox API {response.StatusCode}: {errorBody}");
            }
        }
    }
}
