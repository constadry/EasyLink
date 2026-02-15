using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using EasyLink.Services;
using EasyLink.Models;


var builder = WebApplication.CreateBuilder(args);

var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));

var dbSection = builder.Configuration.GetSection("DB");

var csb = new MySqlConnectionStringBuilder
{
    Server = dbSection["Server"],
    Port = uint.Parse(dbSection["Port"] ?? "3306"),
    Database = dbSection["Database"],
    UserID = dbSection["UserID"],
    Password = dbSection["Password"],
};
var connStr = csb.ConnectionString;

builder.Services.AddDbContext<PurchaseDb>(options =>
    options.UseMySql(connStr, serverVersion));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


var allowedOrigin = builder.Configuration["ALLOWED_ORIGIN"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<TinkoffService>();

// Telegram configuration - читаем из конфигурации (appsettings или env vars)
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));
builder.Services.AddHttpClient<ITelegramService, TelegramService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowLocalFrontend");
}
else
{
    app.UseCors("AllowFrontend");
}

app.MapControllers();

app.MapGet("api/shopitems", async (PurchaseDb db) =>
    await db.ShopItems.Where(s => s.IsActive).ToListAsync())
.WithName("GetShopItems")
.WithOpenApi();

app.MapGet("api/shopitems/{id}", async (int id, PurchaseDb db) =>
    await db.ShopItems.FindAsync(id)
        is ShopItem shopItem
        ? Results.Ok(shopItem)
        : Results.NotFound(new { error = $"Shop item with ID {id} not found" }))
.WithName("GetShopItem")
.WithOpenApi();

app.Run();

/*
docker build -t easylink:dev -f Dockerfile.dev .
docker run --rm -p 8081:8080 -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS=http://+:8080 --name easylink easylink:dev
*/