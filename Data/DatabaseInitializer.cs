using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalAIAssistant.Data;

public class DatabaseInitializer
{
    private readonly LocalAiAssistantDbContext _db;

    public DatabaseInitializer( LocalAiAssistantDbContext db )
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        var version = await GetCurrentVersionAsync();

        var migrations = new List<(int Version, Func<Task> Upgrade)>
                         {
                                 (1, CreateVersion1Async),
                                 // (2, UpgradeToVersion2Async),
                                 // (3, UpgradeToVersion3Async),
                         };

        foreach (var (targetVersion, upgrade) in migrations)
        {
            if (version >= targetVersion) continue;
            
            await ApplyUpgradeAsync(version, targetVersion, upgrade);
            
            version = targetVersion;
        }
    }

    
    private async Task<int> GetCurrentVersionAsync()
    {
        // var connection = _db.Database.GetDbConnection();
        await using var connection = new SqliteConnection(_db.Database.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COUNT(*) 
                                FROM sqlite_master 
                                WHERE type='table' 
                                  AND name='SchemaInfo';";

        var exists = Convert.ToInt32(await command.ExecuteScalarAsync());

        if (exists == 0) return 0;

        command.CommandText = @"SELECT Version 
                                FROM SchemaInfo 
                                WHERE Id = 1;";
        
        var result = await command.ExecuteScalarAsync();

        if (result == null 
         || result == DBNull.Value)
            return 0;

        return Convert.ToInt32(result);
    }


    private async Task CreateVersion1Async()
    {
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS OfflineQueue 
                                                (
                                                      Id TEXT PRIMARY KEY
                                                    , ClientRequestId TEXT NOT NULL
                                                    , SessionId TEXT NOT NULL
                                                    , Input TEXT NOT NULL
                                                    , Model TEXT
                                                    , CreatedUtc TEXT NOT NULL
                                                    , Status INTEGER NOT NULL
                                                    , RetryCount INTEGER NOT NULL
                                                );");

        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS SchemaInfo 
                                                (
                                                      Id INTEGER PRIMARY KEY CHECK (Id = 1)
                                                    , Version INTEGER NOT NULL
                                                );");
    }
    
    private async Task SetVersionAsync(int version)
    {
        await _db.Database.ExecuteSqlRawAsync(@"INSERT OR REPLACE INTO SchemaInfo (Id, Version)
                                                VALUES (1, {0});"
                                                , version);
    }

    private async Task ApplyUpgradeAsync( int        currentVersion
                                        , int        targetVersion
                                        , Func<Task> upgradeAction )
    {
        if (currentVersion >= targetVersion)
            return;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            await upgradeAction();
            await SetVersionAsync(targetVersion);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}