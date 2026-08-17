namespace StockSync.Data;

using Microsoft.Data.Sqlite;
using StockSync.Models;

public class DestinationInventoryRepository
{
    private readonly string _connectionString;

    public DestinationInventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<InventoryItem>> GetAllAsync()
    {
        var items = new List<InventoryItem>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, product_name, quantity, updated_at FROM destination_inventory ORDER BY id ASC;";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new InventoryItem
            {
                Id = reader.GetInt32(0),
                ProductName = reader.GetString(1),
                Quantity = reader.GetInt32(2),
                UpdatedAt = reader.GetString(3)
            });
        }

        return items;
    }

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, product_name, quantity, updated_at FROM destination_inventory WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new InventoryItem
            {
                Id = reader.GetInt32(0),
                ProductName = reader.GetString(1),
                Quantity = reader.GetInt32(2),
                UpdatedAt = reader.GetString(3)
            };
        }

        return null;
    }

    public async Task UpsertAsync(InventoryItem item)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO destination_inventory (id, product_name, quantity, updated_at)
            VALUES (@id, @productName, @quantity, @updatedAt)
            ON CONFLICT(id) DO UPDATE SET
                product_name = excluded.product_name,
                quantity = excluded.quantity,
                updated_at = excluded.updated_at;";

        command.Parameters.AddWithValue("@id", item.Id);
        command.Parameters.AddWithValue("@productName", item.ProductName);
        command.Parameters.AddWithValue("@quantity", item.Quantity);
        command.Parameters.AddWithValue("@updatedAt", item.UpdatedAt);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM destination_inventory WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
