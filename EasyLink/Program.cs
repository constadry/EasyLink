using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));

var csb = new MySqlConnectionStringBuilder
{
    Server = "butcher.eka.s-hub.xyz",
    Port = 3306,
    Database = "s123380_easyeasylinkdb",
    UserID = "u123380_yOK1fFIWEa",
    Password = "=S^SVz!+nOdz9iic84m5xfbV",
};
var connStr = csb.ConnectionString;

// Добавляем DbContext
builder.Services.AddDbContext<PurchaseDb>(options =>
    options.UseMySql(connStr, serverVersion));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseCors("AllowFrontend");

// Эндпоинт для создания покупки
app.MapPost("/purchases", async (PurchaseRequest request, PurchaseDb db) =>
{
    // Проверяем существование услуги
    var service = await db.ShopItems.FindAsync(request.ServiceId);
    if (service == null)
        return Results.NotFound(new { message = "Услуга не найдена" });

    // Создаём покупку
    var purchase = new Purchase
    {
        Email = request.Email,
        Nickname = request.Nickname,
        ShopItemId = request.ServiceId,
        PurchaseDate = DateTime.UtcNow,
        Amount = service.Price
    };

    db.Purchases.Add(purchase);
    await db.SaveChangesAsync();

    return Results.Created($"/purchases/{purchase.Id}", purchase);
})
.WithName("CreatePurchase")
.WithOpenApi();

// Получить все услуги
app.MapGet("/shopitems", async (PurchaseDb db) =>
    await db.ShopItems.Where(s => s.IsActive).ToListAsync())
.WithName("GetShopItems")
.WithOpenApi();

// Получить покупку по ID
app.MapGet("/purchases/{id}", async (int id, PurchaseDb db) =>
    await db.Purchases.Include(p => p.ShopItem).FirstOrDefaultAsync(p => p.Id == id)
        is Purchase purchase
        ? Results.Ok(purchase)
        : Results.NotFound())
.WithName("GetPurchase")
.WithOpenApi();

app.Run();

/*
docker build -t easylink:dev -f Dockerfile.dev .
docker run --rm -p 8081:8080 -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS=http://+:8080 --name easylink easylink:dev
*/