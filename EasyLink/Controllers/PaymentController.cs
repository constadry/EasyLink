
using Microsoft.AspNetCore.Mvc;
using EasyLink.Models;
using EasyLink.Services;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

// Assuming PurchaseDb and Purchase are in global namespace as seen in Program.cs
// If they are in a namespace, this needs to be adjusted.

namespace EasyLink.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly TinkoffService _tinkoffService;
        private readonly PurchaseDb _db;

        public PaymentController(TinkoffService tinkoffService, PurchaseDb db)
        {
            _tinkoffService = tinkoffService;
            _db = db;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] InitPaymentRequest request)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(request.Nick) || string.IsNullOrWhiteSpace(request.Email))
                {
                    // Fallback if client didn't send new fields but structure requires them for DB
                    // For pure payment init example, maybe we can relax this?
                    // But business logic requires Nick/ShopItem.
                    return BadRequest(new { error = "Nick and Email are required" });
                }

                var shopItem = await _db.ShopItems.FindAsync(request.ProductId);
                if (shopItem == null)
                {
                    return NotFound(new { error = "Product not found" });
                }

                // Create Purchase record
                var purchase = new Purchase
                {
                    Email = request.Email,
                    Nickname = request.Nick,
                    ShopItemId = request.ProductId,
                    PurchaseDate = DateTime.UtcNow,
                    Amount = request.Amount > 0 ? request.Amount : shopItem.Price
                };

                _db.Purchases.Add(purchase);
                await _db.SaveChangesAsync();

                var orderId = purchase.Id.ToString();
                // Override request amount with DB amount to ensure consistency? 
                // Or use request amount. Let's use purchase.Amount.
                
                var paymentUrl = await _tinkoffService.InitPaymentUrlAsync(orderId, purchase.Amount, purchase.Email, $"Order #{orderId}");
                return Ok(new { url = paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Webhook called by Tinkoff
        [HttpPost("notify")]
        public async Task<IActionResult> Notification([FromBody] TinkoffNotification notification)
        {
            // 1. Verify token
            if (!_tinkoffService.VerifyNotification(notification))
            {
                return BadRequest("Invalid Token");
            }

            // 2. Update order status
            if (int.TryParse(notification.OrderId, out var purchaseId))
            {
                var purchase = await _db.Purchases.FindAsync(purchaseId);
                if (purchase != null)
                {
                    purchase.Status = notification.Status;
                    if (!string.IsNullOrEmpty(notification.PaymentId))
                    {
                        purchase.PaymentId = notification.PaymentId;
                    }
                    await _db.SaveChangesAsync();
                }
            }

            // 3. Must return "OK" string
            return Content("OK");
        }
    }
}
