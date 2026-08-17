namespace StockSync.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    public string StockStatus => Quantity switch
    {
        0 => "Out of Stock",
        <= 5 => "Low Stock",
        _ => "Normal"
    };
}
