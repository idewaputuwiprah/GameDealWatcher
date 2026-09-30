using Microsoft.Data.Sqlite;

namespace GameDealWatcher.Infrastructure.Database;

public static class DatabaseInitializer
{
    private const int CurrentSchemaVersion = 1;

    public static async Task InitializeAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            -- Stores
            CREATE TABLE IF NOT EXISTS Stores (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                BaseUrl TEXT NOT NULL
            );

            -- GameDeals: current state of deals
            CREATE TABLE IF NOT EXISTS GameDeals (
                Id TEXT PRIMARY KEY,
                ProviderName TEXT NOT NULL,
                ProviderGameId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT,
                Publisher TEXT,
                Developer TEXT,
                OriginalPrice REAL NOT NULL,
                CurrentPrice REAL NOT NULL,
                DiscountPercentage INTEGER NOT NULL,
                Currency TEXT NOT NULL DEFAULT 'USD',
                StoreUrl TEXT NOT NULL,
                ImageUrl TEXT,
                ThumbnailUrl TEXT,
                StartsAt TEXT,
                EndsAt TEXT,
                IsCurrentlyFree INTEGER NOT NULL DEFAULT 0,
                IsUpcoming INTEGER NOT NULL DEFAULT 0,
                ReviewScore INTEGER,
                ReviewCount INTEGER,
                ReleaseDate TEXT,
                Genres TEXT,
                LastUpdated TEXT NOT NULL,
                UNIQUE(ProviderName, ProviderGameId)
            );
            CREATE INDEX IF NOT EXISTS IX_GameDeals_Provider ON GameDeals(ProviderName);
            CREATE INDEX IF NOT EXISTS IX_GameDeals_EndsAt ON GameDeals(EndsAt);
            CREATE INDEX IF NOT EXISTS IX_GameDeals_CurrentPrice ON GameDeals(CurrentPrice);

            -- DealHistory: tracks price changes over time
            CREATE TABLE IF NOT EXISTS DealHistory (
                Id TEXT PRIMARY KEY,
                DealId TEXT NOT NULL,
                OriginalPrice REAL NOT NULL,
                CurrentPrice REAL NOT NULL,
                DiscountPercentage INTEGER NOT NULL,
                Currency TEXT NOT NULL DEFAULT 'USD',
                RecordedAt TEXT NOT NULL,
                FOREIGN KEY (DealId) REFERENCES GameDeals(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_DealHistory_DealId ON DealHistory(DealId);
            CREATE INDEX IF NOT EXISTS IX_DealHistory_RecordedAt ON DealHistory(RecordedAt);

            -- RefreshHistory: audit trail of refresh attempts
            CREATE TABLE IF NOT EXISTS RefreshHistory (
                Id TEXT PRIMARY KEY,
                ProviderName TEXT NOT NULL,
                RefreshedAt TEXT NOT NULL,
                Success INTEGER NOT NULL,
                ItemCount INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_RefreshHistory_Provider ON RefreshHistory(ProviderName);
            CREATE INDEX IF NOT EXISTS IX_RefreshHistory_RefreshedAt ON RefreshHistory(RefreshedAt);

            -- Settings
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            -- Seed stores
            INSERT OR IGNORE INTO Stores (Id, Name, BaseUrl) VALUES
                ('steam', 'Steam', 'https://store.steampowered.com'),
                ('epic', 'Epic Games Store', 'https://www.epicgames.com/store');
        ";
        await cmd.ExecuteNonQueryAsync(ct);

        // Run schema migrations if needed
        await MigrateAsync(connection, ct);
    }

    /// <summary>
    /// Checks the current schema version and applies migrations up to CurrentSchemaVersion.
    /// Future schema changes should add migration steps here.
    /// </summary>
    private static async Task MigrateAsync(SqliteConnection connection, CancellationToken ct)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'SchemaVersion';";
        var result = await cmd.ExecuteScalarAsync(ct);
        var currentVersion = 0;
        if (result != null && result != DBNull.Value)
        {
            int.TryParse(result.ToString(), out currentVersion);
        }

        if (currentVersion >= CurrentSchemaVersion) return;

        // Future migrations go here:
        // if (currentVersion < 2) { ... run ALTER TABLE ...; currentVersion = 2; }

        // Record the schema version
        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = "INSERT INTO Settings (Key, Value) VALUES ('SchemaVersion', @Version) ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        var pVersion = updateCmd.CreateParameter();
        pVersion.ParameterName = "@Version";
        pVersion.Value = CurrentSchemaVersion.ToString();
        updateCmd.Parameters.Add(pVersion);
        await updateCmd.ExecuteNonQueryAsync(ct);
    }
}