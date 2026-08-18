namespace StockSync.Tests;

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using StockSync.Data;
using StockSync.Models;
using StockSync.Services;

public class InventorySyncTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly SourceInventoryRepository _sourceRepo;
    private readonly DestinationInventoryRepository _destRepo;
    private readonly SyncHistoryRepository _historyRepo;
    private readonly InventorySyncService _syncService;

    public InventorySyncTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"stocksync_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";

        var initializer = new DatabaseInitializer(_connectionString, _dbPath);
        initializer.Initialize();

        _sourceRepo = new SourceInventoryRepository(_connectionString);
        _destRepo = new DestinationInventoryRepository(_connectionString);
        _historyRepo = new SyncHistoryRepository(_connectionString);
        _syncService = new InventorySyncService(_sourceRepo, _destRepo, _historyRepo);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Test1_NewProductIsDetected_AndAddedToDestination()
    {
        // Arrange
        var newProduct = await _sourceRepo.CreateAsync("Green Jacket", 7);

        // Act
        var result = await _syncService.ExecuteSyncAsync();

        // Assert
        Assert.Equal(1, result.ItemsAdded);
        var destItem = await _destRepo.GetByIdAsync(newProduct.Id);
        Assert.NotNull(destItem);
        Assert.Equal("Green Jacket", destItem.ProductName);
        Assert.Equal(7, destItem.Quantity);
    }

    [Fact]
    public async Task Test2_ChangedQuantityIsDetected_AndUpdateApplied()
    {
        // Initial state has Black Shirt (Id=2): Source=15, Dest=10
        var result = await _syncService.ExecuteSyncAsync();

        // Assert
        Assert.Equal(1, result.ItemsChanged);
        var updatedDestItem = await _destRepo.GetByIdAsync(2);
        Assert.NotNull(updatedDestItem);
        Assert.Equal(15, updatedDestItem.Quantity);
    }

    [Fact]
    public async Task Test3_UnchangedProductIsIgnored()
    {
        // Sync once to bring dest in line with source
        await _syncService.ExecuteSyncAsync();

        // Act - Sync again with no source changes
        var result = await _syncService.ExecuteSyncAsync();

        // Assert
        Assert.Equal(0, result.ItemsChanged);
        Assert.Equal(3, result.ItemsUnchanged);
    }

    [Fact]
    public async Task Test4_SourceQuantityIsCopiedToDestination()
    {
        // Change source quantity: Blue Shoes (Id=1) 20 -> 3
        await _sourceRepo.UpdateQuantityAsync(1, 3);

        // Act
        await _syncService.ExecuteSyncAsync();

        // Assert
        var destItem = await _destRepo.GetByIdAsync(1);
        Assert.NotNull(destItem);
        Assert.Equal(3, destItem.Quantity);
    }

    [Fact]
    public async Task Test5_SynchronizationHistoryIsRecorded()
    {
        // Act - initial sync changes Black Shirt from 10 to 15
        await _syncService.ExecuteSyncAsync();

        // Assert
        var history = await _historyRepo.GetRecentAsync(10);
        Assert.NotEmpty(history);
        var blackShirtChange = history.Find(c => c.ProductId == 2);
        Assert.NotNull(blackShirtChange);
        Assert.Equal(10, blackShirtChange.PreviousQuantity);
        Assert.Equal(15, blackShirtChange.NewQuantity);
    }

    [Fact]
    public void Test6_LowStockClassificationWorks()
    {
        var outOfStock = new InventoryItem { Quantity = 0 };
        var lowStock = new InventoryItem { Quantity = 4 };
        var normalStock = new InventoryItem { Quantity = 12 };

        Assert.Equal("Out of Stock", outOfStock.StockStatus);
        Assert.Equal("Low Stock", lowStock.StockStatus);
        Assert.Equal("Normal", normalStock.StockStatus);
    }

    [Fact]
    public async Task Test7_InitialDatabaseSeed_DoesNotDuplicateOnRestart()
    {
        var initializer = new DatabaseInitializer(_connectionString, _dbPath);
        initializer.Initialize();

        var sourceItems = await _sourceRepo.GetAllAsync();
        var destItems = await _destRepo.GetAllAsync();

        Assert.Equal(3, sourceItems.Count);
        Assert.Equal(3, destItems.Count);
    }
}
