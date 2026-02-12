
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
                    Amount = request.Amount,
                    Status = "PENDING"
                };

                _db.Purchases.Add(purchase);
                await _db.SaveChangesAsync();

                var orderId = purchase.Id.ToString();
                
                var paymentUrl = await _tinkoffService.InitPaymentUrlAsync(orderId, shopItem.Price, purchase.Email, $"Order #{orderId}", request.Amount);
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

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelPayment([FromBody] CancelRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PaymentId))
                {
                    return BadRequest(new { error = "PaymentId is required" });
                }

                // If items are provided, ensure amounts match
                if (request.Items != null && request.Items.Count > 0 && request.Amount.HasValue)
                {
                    var totalItemsAmount = 0;
                    foreach (var item in request.Items)
                    {
                        totalItemsAmount += item.Amount;
                    }
                    
                    if (totalItemsAmount != (int)(request.Amount.Value * 100))
                    {
                        // This is a warning, depending on business logic you might want to fail
                        // or just log it. Tinkoff will validate this anyway.
                    }
                }

                var result = await _tinkoffService.CancelPaymentAsync(request.PaymentId, request.Amount, request.Items, request.Email, request.Taxation);
                
                // You might want to update the database here as well if the cancellation is successful
                // e.g., mark the purchase as Refunded. 
                // However, doing it in the webhook is more robust.
                
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("purchases")]
        public async Task<IActionResult> GetAllPurchases([FromQuery] PaginationRequest pagination)
        {
            try
            {
                var purchases = await _db.Purchases
                    .Include(p => p.ShopItem)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var result = new PaginationResult<Purchase>
                {
                    Items = purchases,
                    Pagination = new Pagination
                    {
                        Page = pagination.Page,
                        PageSize = pagination.PageSize,
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
