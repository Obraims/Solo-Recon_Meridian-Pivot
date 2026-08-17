namespace StockSync.Data;

using Microsoft.Data.Sqlite;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly string _dbFilePath;

    public DatabaseInitializer(string connectionString, string dbFilePath)
    {
        _connectionString = connectionString;
        _dbFilePath = dbFilePath;
    }

    public void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // 1. Create source_inventory table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS source_inventory (
                    id INTEGER PRIMARY KEY,
                    product_name TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    updated_at TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // 2. Create destination_inventory table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS destination_inventory (
                    id INTEGER PRIMARY KEY,
                    product_name TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    updated_at TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // 3. Create sync_history table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS sync_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    product_id INTEGER NOT NULL,
                    product_name TEXT NOT NULL,
                    previous_quantity INTEGER,
                    new_quantity INTEGER,
                    change_type TEXT NOT NULL,
                    synced_at TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // Seed initial data if tables are empty
        SeedInitialData(connection, transaction);

        transaction.Commit();
    }

    private void SeedInitialData(SqliteConnection connection, SqliteTransaction transaction)
    {
        long sourceCount = 0;
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT COUNT(*) FROM source_inventory;";
            sourceCount = (long)(checkCmd.ExecuteScalar() ?? 0L);
        }

        long destCount = 0;
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT COUNT(*) FROM destination_inventory;";
            destCount = (long)(checkCmd.ExecuteScalar() ?? 0L);
        }

        var now = DateTime.UtcNow.ToString("o");

        if (sourceCount == 0)
        {
            var sourceSeed = new (int id, string name, int qty)[]
            {
                (1, "Blue Shoes", 20),
                (2, "Black Shirt", 15),
                (3, "Red Cap", 8)
            };

            foreach (var item in sourceSeed)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO source_inventory (id, product_name, quantity, updated_at)
                    VALUES (@id, @name, @qty, @updatedAt);";
                insertCmd.Parameters.AddWithValue("@id", item.id);
                insertCmd.Parameters.AddWithValue("@name", item.name);
                insertCmd.Parameters.AddWithValue("@qty", item.qty);
                insertCmd.Parameters.AddWithValue("@updatedAt", now);
                insertCmd.ExecuteNonQuery();
            }
        }

        if (destCount == 0)
        {
            // Initial destination has Black Shirt = 10 so synchronization can be demonstrated
            var destSeed = new (int id, string name, int qty)[]
            {
                (1, "Blue Shoes", 20),
                (2, "Black Shirt", 10),
                (3, "Red Cap", 8)
            };

            foreach (var item in destSeed)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO destination_inventory (id, product_name, quantity, updated_at)
                    VALUES (@id, @name, @qty, @updatedAt);";
                insertCmd.Parameters.AddWithValue("@id", item.id);
                insertCmd.Parameters.AddWithValue("@name", item.name);
                insertCmd.Parameters.AddWithValue("@qty", item.qty);
                insertCmd.Parameters.AddWithValue("@updatedAt", now);
                insertCmd.ExecuteNonQuery();
            }
        }
    }
}
