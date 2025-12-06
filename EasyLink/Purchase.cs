public class Purchase
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int ShopItemId { get; set; }
    public ShopItem? ShopItem { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
}
