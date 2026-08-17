namespace StockSync.Services;

using StockSync.Data;
using StockSync.Models;

public class InventorySyncService
{
    private readonly SourceInventoryRepository _sourceRepo;
    private readonly DestinationInventoryRepository _destRepo;
    private readonly SyncHistoryRepository _historyRepo;

    public InventorySyncService(
        SourceInventoryRepository sourceRepo,
        DestinationInventoryRepository destRepo,
        SyncHistoryRepository historyRepo)
    {
        _sourceRepo = sourceRepo;
        _destRepo = destRepo;
        _historyRepo = historyRepo;
    }

    public async Task<SyncResult> ExecuteSyncAsync()
    {
        var sourceItems = await _sourceRepo.GetAllAsync();
        var destItems = await _destRepo.GetAllAsync();

        var sourceDict = sourceItems.ToDictionary(i => i.Id);
        var destDict = destItems.ToDictionary(i => i.Id);

        var nowStr = DateTime.UtcNow.ToString("o");

        var result = new SyncResult
        {
            Success = true,
            SyncedAt = nowStr,
            ItemsChecked = sourceItems.Count
        };

        // 1. Process items in source (Check for ADDED, UPDATED, UNCHANGED)
        foreach (var sourceItem in sourceItems)
        {
            if (!destDict.TryGetValue(sourceItem.Id, out var destItem))
            {
                // ADDED: Exists in source, missing in destination
                var newDestItem = new InventoryItem
                {
                    Id = sourceItem.Id,
                    ProductName = sourceItem.ProductName,
                    Quantity = sourceItem.Quantity,
                    UpdatedAt = nowStr
                };
                await _destRepo.UpsertAsync(newDestItem);

                var change = new InventoryChange
                {
                    ProductId = sourceItem.Id,
                    ProductName = sourceItem.ProductName,
                    PreviousQuantity = null,
                    NewQuantity = sourceItem.Quantity,
                    ChangeType = "ADDED",
                    SyncedAt = nowStr
                };
                await _historyRepo.CreateAsync(change);

                result.ItemsAdded++;
                result.ItemsChanged++;
                result.Changes.Add(change);
            }
            else if (sourceItem.Quantity != destItem.Quantity || sourceItem.ProductName != destItem.ProductName)
            {
                // UPDATED: Exists in both, but quantity or name changed
                var prevQty = destItem.Quantity;

                destItem.ProductName = sourceItem.ProductName;
                destItem.Quantity = sourceItem.Quantity;
                destItem.UpdatedAt = nowStr;

                await _destRepo.UpsertAsync(destItem);

                var change = new InventoryChange
                {
                    ProductId = sourceItem.Id,
                    ProductName = sourceItem.ProductName,
                    PreviousQuantity = prevQty,
                    NewQuantity = sourceItem.Quantity,
                    ChangeType = "UPDATED",
                    SyncedAt = nowStr
                };
                await _historyRepo.CreateAsync(change);

                result.ItemsChanged++;
                result.Changes.Add(change);
            }
            else
            {
                // UNCHANGED: Identical in both
                var change = new InventoryChange
                {
                    ProductId = sourceItem.Id,
                    ProductName = sourceItem.ProductName,
                    PreviousQuantity = destItem.Quantity,
                    NewQuantity = sourceItem.Quantity,
                    ChangeType = "UNCHANGED",
                    SyncedAt = nowStr
                };
                await _historyRepo.CreateAsync(change);

                result.ItemsUnchanged++;
            }
        }

        // 2. Process items in destination that are missing in source (REMOVED)
        foreach (var destItem in destItems)
        {
            if (!sourceDict.ContainsKey(destItem.Id))
            {
                await _destRepo.DeleteAsync(destItem.Id);

                var change = new InventoryChange
                {
                    ProductId = destItem.Id,
                    ProductName = destItem.ProductName,
                    PreviousQuantity = destItem.Quantity,
                    NewQuantity = null,
                    ChangeType = "REMOVED",
                    SyncedAt = nowStr
                };
                await _historyRepo.CreateAsync(change);

                result.ItemsRemoved++;
                result.ItemsChanged++;
                result.Changes.Add(change);
            }
        }

        return result;
    }
}
