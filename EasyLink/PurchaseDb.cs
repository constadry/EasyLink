using Microsoft.EntityFrameworkCore;

public class PurchaseDb : DbContext
{
    public PurchaseDb(DbContextOptions<PurchaseDb> options) : base(options) { }

    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
}
