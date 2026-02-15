namespace EasyLink.Models;

public class PurchasesFilterRequest : PaginationRequest
{
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public int? ShopItemId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? Amount { get; set; }
    public bool? Delivered { get; set; }
    public int? Duration { get; set; }
}
