namespace StockSync.Models;

public class InventoryChange
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? PreviousQuantity { get; set; }
    public int? NewQuantity { get; set; }
    public string ChangeType { get; set; } = string.Empty; // ADDED, UPDATED, UNCHANGED, REMOVED
    public string SyncedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
