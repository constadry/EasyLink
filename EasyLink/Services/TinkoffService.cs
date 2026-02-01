
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyLink.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace EasyLink.Services
{
    public class TinkoffService
    {
        private readonly string _terminalKey;
        private readonly string _password;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TinkoffService> _logger;

        public TinkoffService(HttpClient httpClient, IConfiguration config, ILogger<TinkoffService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _terminalKey = config["TBank:TerminalKey"];
            _password = config["TBank:Password"];
        }


        public async Task<string> InitPaymentUrlAsync(string orderId, decimal amount, string email, string description)
        {
            var requestData = new SortedDictionary<string, object>
            {
                { "TerminalKey", _terminalKey },
                { "Amount", (int)(amount * 100) }, // Convert to kopecks
                { "OrderId", orderId },
                { "Description", description },
                { "Password", _password } // Password is required for token generation
            };

            // Generate signature (Token)
            var token = GenerateToken(requestData);

            // Remove Password before sending to API (Security requirement)
            requestData.Remove("Password");
            
            // Add Token to request
            requestData.Add("Token", token);
            
            // Basic Init adds no extra fields usually, but receipt might be needed later.
            // For now, following the user's simple flow.

            var jsonRequest = JsonSerializer.Serialize(requestData);
            _logger.LogInformation("Tinkoff Init Request: {Request}", jsonRequest);

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("https://securepay.tinkoff.ru/v2/Init", content);
            var responseString = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Tinkoff Init Response: {Response}", responseString);

            
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("Success", out var success) && success.GetBoolean())
            {
                return doc.RootElement.GetProperty("PaymentURL").GetString();
            }

            var message = doc.RootElement.TryGetProperty("Message", out var msg) ? msg.GetString() : "Unknown error";
            var details = doc.RootElement.TryGetProperty("Details", out var det) ? det.GetString() : "";
            throw new Exception($"Tinkoff Error: {message} {details}");
        }

        public bool VerifyNotification(TinkoffNotification notification)
        {
            // Convert notification object to dictionary for hashing
            var args = new SortedDictionary<string, object>
            {
                { "TerminalKey", _terminalKey },
                { "OrderId", notification.OrderId },
                { "Success", notification.Success ? "true" : "false" }, // Boolean to lowercase string
                { "Status", notification.Status },
                { "PaymentId", notification.PaymentId },
                { "ErrorCode", notification.ErrorCode },
                { "Amount", notification.Amount },
                { "Password", _password } // Add password for check
            };

            // Add optional fields only if they exist in the incoming request
            if (notification.CardId != null) args.Add("CardId", notification.CardId);
            if (!string.IsNullOrEmpty(notification.Pan)) args.Add("Pan", notification.Pan);
            if (!string.IsNullOrEmpty(notification.ExpDate)) args.Add("ExpDate", notification.ExpDate);

            var calculatedToken = GenerateToken(args);

            // Compare calculated token with the one from Tinkoff
            return calculatedToken == notification.Token;
        }

        private string GenerateToken(SortedDictionary<string, object> args)
        {
            // 1. Sort is handled by SortedDictionary
            // 2. Concatenate values
            var sb = new StringBuilder();
            foreach (var key in args.Keys)
            {
                sb.Append(args[key]);
            }

            // 3. SHA-256 Hash
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
