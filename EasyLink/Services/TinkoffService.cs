
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


        public async Task<string> InitPaymentUrlAsync(string orderId, decimal amount, string email, string description, string taxation = "usn_income")
        {
            var amountInKopecks = (int)(amount * 100);
            
            // Create receipt item for fiscalization (54-ФЗ)
            var receiptItem = new ReceiptItem
            {
                Name = description.Length > 64 ? description.Substring(0, 64) : description, // Max 64 chars per Tinkoff docs
                Price = amountInKopecks,
                Quantity = 1,
                Amount = amountInKopecks,
                Tax = "none" // "none", "vat0", "vat10", "vat20" etc.
            };

            var receipt = new Receipt
            {
                Email = email,
                Taxation = taxation, // "osn", "usn_income", "usn_income_outcome", "envd", "esn", "patent"
                Items = new List<ReceiptItem> { receiptItem }
            };

            var requestData = new SortedDictionary<string, object>
            {
                { "TerminalKey", _terminalKey },
                { "Amount", amountInKopecks },
                { "OrderId", orderId },
                { "Description", description },
                { "Password", _password } // Password is required for token generation
            };

            // Generate signature (Token) - Receipt and DATA are excluded from token generation
            var token = GenerateToken(requestData);

            // Remove Password before sending to API (Security requirement)
            requestData.Remove("Password");
            
            // Add Token to request
            requestData.Add("Token", token);
            
            // Add Receipt object (required for fiscalization)
            requestData.Add("Receipt", receipt);

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

        public async Task<string> CancelPaymentAsync(string paymentId, decimal? amount, List<ReceiptItem> items, string email, string taxation)
        {
            var requestData = new SortedDictionary<string, object>
            {
                { "TerminalKey", _terminalKey },
                { "PaymentId", paymentId },
                { "Password", _password }
            };

            if (amount.HasValue)
            {
                requestData.Add("Amount", (int)(amount.Value * 100)); // Convert to kopecks
            }

            // --- IMPORTANT: Receipt Logic ---
            // If you have items, you MUST construct the Receipt object
            if (items != null && items.Count > 0)
            {
                var receipt = new Receipt
                {
                    Email = email,
                    Taxation = taxation ?? "usn_income", // Use passed value or fallback
                    Items = items
                };
                
                // Tinkoff expects nested JSON objects, but they are NOT part of the Token generation usually.
                // However, they must be in the body.
                requestData.Add("Receipt", receipt);
            }

            // 1. Generate Token (Receipt object is usually EXCLUDED from token generation logic in V2, 
            // but verify with documentation if you get "Token Incorrect". Usually only top-level scalar fields are hashed).
            // We create a separate dictionary for hashing to be safe.
            var argsForToken = new SortedDictionary<string, object>();
            foreach(var kvp in requestData) {
                if (kvp.Key != "Receipt" && kvp.Key != "DATA") {
                    argsForToken.Add(kvp.Key, kvp.Value);
                }
            }
            
            var token = GenerateToken(argsForToken); // Use the method from previous answer

            // 2. Prepare final payload
            requestData.Remove("Password");
            requestData.Add("Token", token);

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://securepay.tinkoff.ru/v2/Cancel", content);
            
            return await response.Content.ReadAsStringAsync();
        }
    }
}
