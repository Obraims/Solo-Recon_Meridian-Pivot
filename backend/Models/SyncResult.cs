namespace StockSync.Models;

public class SyncResult
{
    public bool Success { get; set; } = true;
    public int ItemsChecked { get; set; }
    public int ItemsChanged { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsUnchanged { get; set; }
    public int ItemsRemoved { get; set; }
    public string SyncedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public List<InventoryChange> Changes { get; set; } = new();
}
