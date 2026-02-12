
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EasyLink.Models
{
    public class InitPaymentRequest
    {
        public string OrderId { get; set; }
        public int Price { get; set; } // Price in kopecks (rubles * 100)
        public int Amount { get; set; }
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
        public int Price { get; set; }
        public int? CardId { get; set; }
        public string Pan { get; set; }
        public string ExpDate { get; set; }
        public string Token { get; set; }
    }

    public class CancelRequest
    {
        public string PaymentId { get; set; }
        public decimal? Amount { get; set; } // Optional: specific amount to refund
        public List<ReceiptItem> Items { get; set; } // List of items to return
        public string Email { get; set; }
        public string Taxation { get; set; } // Added taxation field
    }

    // Structure required by Tinkoff for 54-FZ
    public class Receipt
    {
        public string Email { get; set; }
        public string Taxation { get; set; } // e.g., "osn", "usn_income"
        public List<ReceiptItem> Items { get; set; }
    }

    public class ReceiptItem
    {
        public string Name { get; set; }
        public int Price { get; set; }    // Price in kopecks
        public int Quantity { get; set; } // Quantity (usually 1.00 -> 1)
        public int Amount { get; set; }   // Total amount (Price * Quantity) in kopecks
        public string Tax { get; set; }   // e.g., "vat20", "none"
        public string PaymentMethod { get; set; } = "full_payment"; // full_prepayment, prepayment, advance, full_payment, etc.
        public string PaymentObject { get; set; } = "service"; // commodity, excise, job, service, payment, etc.
    }
}
