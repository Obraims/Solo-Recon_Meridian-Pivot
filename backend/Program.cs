using Microsoft.Extensions.FileProviders;
using StockSync.Data;
using StockSync.Models;
using StockSync.Services;

var builder = WebApplication.CreateBuilder(args);

string FindFrontendDirectory()
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend")),
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "frontend")),
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "frontend")),
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frontend")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "frontend")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend"))
    };

    foreach (var dir in candidates)
    {
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "index.html")))
        {
            return dir;
        }
    }
    return candidates[0];
}

var frontendPath = FindFrontendDirectory();

string FindDatabasePath()
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "database")),
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "database")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "database")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "database"))
    };

    foreach (var dir in candidates)
    {
        if (Directory.Exists(dir)) return Path.Combine(dir, "stocksync.db");
    }

    var defaultDbDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "database"));
    Directory.CreateDirectory(defaultDbDir);
    return Path.Combine(defaultDbDir, "stocksync.db");
}

var dbPath = FindDatabasePath();
var connectionString = $"Data Source={dbPath}";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton(new DatabaseInitializer(connectionString, dbPath));
builder.Services.AddScoped(_ => new SourceInventoryRepository(connectionString));
builder.Services.AddScoped(_ => new DestinationInventoryRepository(connectionString));
builder.Services.AddScoped(_ => new SyncHistoryRepository(connectionString));
builder.Services.AddScoped<InventorySyncService>();

var app = builder.Build();

app.UseCors();

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    initializer.Initialize();
}

if (Directory.Exists(frontendPath))
{
    var fileProvider = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}

app.MapGet("/", () =>
{
    var indexPath = Path.Combine(frontendPath, "index.html");
    return File.Exists(indexPath)
        ? Results.File(indexPath, "text/html")
        : Results.Ok(new { system = "StockSync Inventory Synchronization", status = "Healthy" });
});

app.MapGet("/api/status", async (
    SourceInventoryRepository sourceRepo,
    DestinationInventoryRepository destRepo) =>
{
    var sourceItems = await sourceRepo.GetAllAsync();
    var destItems = await destRepo.GetAllAsync();

    var destDict = destItems.ToDictionary(i => i.Id);

    int inSync = 0;
    int needsSync = 0;

    foreach (var s in sourceItems)
    {
        if (destDict.TryGetValue(s.Id, out var d) && d.Quantity == s.Quantity && d.ProductName == s.ProductName)
        {
            inSync++;
        }
        else
        {
            needsSync++;
        }
    }

    bool isSynced = needsSync == 0 && sourceItems.Count == destItems.Count;

    return Results.Ok(new
    {
        isSynchronized = isSynced,
        totalProducts = sourceItems.Count,
        inSyncCount = inSync,
        needsSyncCount = needsSync,
        serviceStatus = "Online"
    });
});

app.MapGet("/source/inventory", async (SourceInventoryRepository repo) =>
{
    var items = await repo.GetAllAsync();
    return Results.Ok(items);
});

app.MapGet("/source/inventory/{id:int}", async (int id, SourceInventoryRepository repo) =>
{
    var item = await repo.GetByIdAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { message = $"Source item {id} not found." });
});

app.MapPut("/source/inventory/{id:int}", async (int id, UpdateQuantityRequest req, SourceInventoryRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(new { message = $"Source item {id} not found." });
    }

    bool updated = await repo.UpdateQuantityAsync(id, req.Quantity);
    var item = await repo.GetByIdAsync(id);
    return updated && item != null ? Results.Ok(item) : Results.BadRequest(new { message = "Failed to update item." });
});

app.MapPost("/source/inventory", async (CreateItemRequest req, SourceInventoryRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.ProductName))
    {
        return Results.BadRequest(new { message = "Product name is required." });
    }
    var item = await repo.CreateAsync(req.ProductName.Trim(), req.Quantity);
    return Results.Created($"/source/inventory/{item.Id}", item);
});

app.MapDelete("/source/inventory/{id:int}", async (int id, SourceInventoryRepository repo) =>
{
    bool deleted = await repo.DeleteAsync(id);
    return deleted ? Results.Ok(new { message = $"Item {id} deleted." }) : Results.NotFound(new { message = $"Item {id} not found." });
});

app.MapGet("/destination/inventory", async (DestinationInventoryRepository repo) =>
{
    var items = await repo.GetAllAsync();
    return Results.Ok(items);
});

app.MapPost("/sync", async (InventorySyncService syncService) =>
{
    var result = await syncService.ExecuteSyncAsync();
    return Results.Ok(result);
});

app.MapGet("/sync/history", async (SyncHistoryRepository repo) =>
{
    var history = await repo.GetRecentAsync(50);
    return Results.Ok(history);
});

app.Run();

public record UpdateQuantityRequest(int Quantity, string? ProductName = null);
public record CreateItemRequest(string ProductName, int Quantity);

public partial class Program { }
