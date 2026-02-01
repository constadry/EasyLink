
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EasyLink.Models
{
    public class InitPaymentRequest
    {
        public string OrderId { get; set; }
        public int Amount { get; set; } // Amount in kopecks (rubles * 100)
        public string Description { get; set; }
        public string Email { get; set; }
        // Added these to support existing logic if needed, but keeping user's core structure
        public string Nick { get; set; } 
        public int ProductId { get; set; }
    }

    public class TinkoffInitResponse
    {
        public bool Success { get; set; }
        public string PaymentURL { get; set; }
        public string PaymentId { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }

    public class TinkoffNotification
    {
        public string TerminalKey { get; set; }
        public string OrderId { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string PaymentId { get; set; }
        public string ErrorCode { get; set; }
        public int Amount { get; set; }
        public int? CardId { get; set; }
        public string Pan { get; set; }
        public string ExpDate { get; set; }
        public string Token { get; set; }
    }
}
