using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace UserManagementApp.Services
{
    public class SalesforceService : ISalesforceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public SalesforceService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private string GetConfigValue(string keyName)
        {
            // Пытаемся взять значение из корня (например, из переменных окружения в Render: Salesforce__ClientId)
            var val = _configuration[$"Salesforce:{keyName}"];
            
            // Если там не пусто, и это не тестовая заглушка, возвращаем
            if (!string.IsNullOrEmpty(val) && !val.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("OUR_", StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }

            // Иначе ищем во вложенной секции (Authentication:Salesforce:...)
            val = _configuration[$"Authentication:Salesforce:{keyName}"];
            if (!string.IsNullOrEmpty(val) && !val.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("OUR_", StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }

            return null;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            var clientId = GetConfigValue("ClientId");
            var clientSecret = GetConfigValue("ClientSecret");
            var username = GetConfigValue("Username");
            var password = GetConfigValue("Password");

            if (string.IsNullOrEmpty(clientId))
            {
                throw new Exception("Salesforce ClientId is missing or contains 'YOUR_...'. Please check your configuration.");
            }

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await _httpClient.PostAsync("https://login.salesforce.com/services/oauth2/token", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                var shortId = clientId.Length > 5 ? clientId.Substring(0, 5) + "..." : clientId;
                throw new Exception($"Failed to authenticate with Salesforce (Client ID: {shortId}): {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            
            _tokenExpiry = DateTime.UtcNow.AddHours(1);

            return _accessToken;
        }

        public async Task<string> CreateAccountAsync(string name, string phone = null, string website = null)
        {
            var token = await GetAccessTokenAsync();
            
            // Поддерживаем оба варианта названия: InstanceUrl и LoginUrl
            var instanceUrl = GetConfigValue("InstanceUrl") ?? GetConfigValue("LoginUrl");
            
            if (string.IsNullOrEmpty(instanceUrl)) throw new Exception("Salesforce InstanceUrl is missing from configuration.");

            var accountData = new
            {
                Name = name,
                Phone = phone,
                Website = website
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{instanceUrl}/services/data/v59.0/sobjects/Account/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(accountData), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create Salesforce Account: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }

        public async Task<string> CreateContactAsync(string accountId, string firstName, string lastName, string email, string title = null, string department = null)
        {
            var token = await GetAccessTokenAsync();
            var instanceUrl = GetConfigValue("InstanceUrl") ?? GetConfigValue("LoginUrl");
            
            var contactData = new
            {
                AccountId = accountId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Title = title,
                Department = department
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{instanceUrl}/services/data/v59.0/sobjects/Contact/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(contactData), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create Salesforce Contact: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }
    }
}
