namespace StockSync.Data;

using Microsoft.Data.Sqlite;
using StockSync.Models;

public class SourceInventoryRepository
{
    private readonly string _connectionString;

    public SourceInventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<InventoryItem>> GetAllAsync()
    {
        var items = new List<InventoryItem>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, product_name, quantity, updated_at FROM source_inventory ORDER BY id ASC;";

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
        command.CommandText = "SELECT id, product_name, quantity, updated_at FROM source_inventory WHERE id = @id;";
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

    public async Task<InventoryItem> CreateAsync(string productName, int quantity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var updatedAt = DateTime.UtcNow.ToString("o");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO source_inventory (product_name, quantity, updated_at)
            VALUES (@productName, @quantity, @updatedAt);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@productName", productName);
        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@updatedAt", updatedAt);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new InventoryItem
        {
            Id = newId,
            ProductName = productName,
            Quantity = quantity,
            UpdatedAt = updatedAt
        };
    }

    public async Task<bool> UpdateQuantityAsync(int id, int newQuantity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var updatedAt = DateTime.UtcNow.ToString("o");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE source_inventory
            SET quantity = @quantity, updated_at = @updatedAt
            WHERE id = @id;";
        command.Parameters.AddWithValue("@quantity", newQuantity);
        command.Parameters.AddWithValue("@updatedAt", updatedAt);
        command.Parameters.AddWithValue("@id", id);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM source_inventory WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
