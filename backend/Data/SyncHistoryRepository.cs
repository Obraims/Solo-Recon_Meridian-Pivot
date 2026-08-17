namespace StockSync.Data;

using Microsoft.Data.Sqlite;
using StockSync.Models;

public class SyncHistoryRepository
{
    private readonly string _connectionString;

    public SyncHistoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> CreateAsync(InventoryChange change)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO sync_history (product_id, product_name, previous_quantity, new_quantity, change_type, synced_at)
            VALUES (@productId, @productName, @prevQty, @newQty, @changeType, @syncedAt);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@productId", change.ProductId);
        command.Parameters.AddWithValue("@productName", change.ProductName);
        command.Parameters.AddWithValue("@prevQty", (object?)change.PreviousQuantity ?? DBNull.Value);
        command.Parameters.AddWithValue("@newQty", (object?)change.NewQuantity ?? DBNull.Value);
        command.Parameters.AddWithValue("@changeType", change.ChangeType);
        command.Parameters.AddWithValue("@syncedAt", change.SyncedAt);

        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        change.Id = id;
        return id;
    }

    public async Task<List<InventoryChange>> GetRecentAsync(int limit = 50)
    {
        var list = new List<InventoryChange>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, product_id, product_name, previous_quantity, new_quantity, change_type, synced_at
            FROM sync_history
            ORDER BY id DESC
            LIMIT @limit;";
        command.Parameters.AddWithValue("@limit", limit);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new InventoryChange
            {
                Id = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                ProductName = reader.GetString(2),
                PreviousQuantity = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                NewQuantity = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                ChangeType = reader.GetString(5),
                SyncedAt = reader.GetString(6)
            });
        }

        return list;
    }
}
