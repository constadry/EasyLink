public class ShopItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public uint Amount { get; set; } = 0;
    public bool IsActive { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
}
