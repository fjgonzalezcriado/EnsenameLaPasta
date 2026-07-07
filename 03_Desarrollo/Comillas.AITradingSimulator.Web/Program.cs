using System.Text.Json;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Asegurar carpeta App_Data en runtime (SQLite intentará crear el .db dentro)
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Aplicar migraciones automáticamente al arrancar (aceptable en app personal local).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    await db.Database.MigrateAsync();

    // Semilla de la watchlist: si está vacía, seguimos HY9H.F (SK hynix Inc., Frankfurt).
    if (!await db.TrackedSymbols.AnyAsync())
    {
        db.TrackedSymbols.Add(TrackedSymbol.Create("HY9H.F", "SK hynix Inc.", DateTime.UtcNow));
        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
