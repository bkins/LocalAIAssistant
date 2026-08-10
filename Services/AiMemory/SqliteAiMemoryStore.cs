using System.Runtime.CompilerServices;
using LocalAIAssistant.Core.Data;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalAIAssistant.Services.AiMemory;

public class SqliteAiMemoryStore : IShortTermMemoryStore
{
    private readonly ILoggingService _loggingService;

    private readonly string _dbPath;
    private readonly string _connectionString;
    private const    string TableName = StringConsts.AiMemoryTableName;

    public SqliteAiMemoryStore(ILoggingService loggingService
                             , string          dbPath)
    {
        _loggingService = loggingService;

        // _dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory
        //                                , "AiMemory.db");
        _dbPath = dbPath;

        // BACK-08: route through the shared LAA.Core helper instead of
        // interpolating _dbPath into the connection string directly.
        _connectionString = SqliteConnectionStrings.ForDataSource(_dbPath);

        EnsureDatabase();
        _loggingService.LogInformation($"Database created at: {_dbPath}");
    }

    private void EnsureDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {TableName} (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        Content TEXT NOT NULL,
                        Sender TEXT NOT NULL
                    );";
            command.ExecuteNonQuery();
        }

        // Migrate schema by adding new telemetry columns if they don't exist
        var columnsToAdd = new Dictionary<string, string>
                           {
                               { "WasFastPath", "INTEGER DEFAULT 0" }
                             , { "Provider", "TEXT NULL" }
                             , { "Model", "TEXT NULL" }
                             , { "ResponseDurationMs", "REAL DEFAULT 0" }
                           };

        foreach (var col in columnsToAdd)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE {TableName} ADD COLUMN {col.Key} {col.Value};";
                command.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // Column already exists or table locked (SQLITE_ERROR)
            {
                // Column already exists, safe to swallow
            }
        }
    }

    public async Task SaveMessagesAsync(IEnumerable<Message> messages)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        foreach (var msg in messages)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                    INSERT INTO {TableName} (Timestamp, Content, Sender, WasFastPath, Provider, Model, ResponseDurationMs) 
                    VALUES (@ts, @content, @sender, @wasFastPath, @provider, @model, @duration);";
            cmd.Parameters.AddWithValue("@ts"
                                       , msg.Timestamp.ToString("o")); // ISO 8601 format
            cmd.Parameters.AddWithValue("@content"
                                       , msg.Content);
            cmd.Parameters.AddWithValue("@sender"
                                       , msg.Sender);
            cmd.Parameters.AddWithValue("@wasFastPath"
                                       , msg.WasFastPath ? 1 : 0);
            cmd.Parameters.AddWithValue("@provider"
                                       , (object?)msg.Provider ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@model"
                                       , (object?)msg.Model ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@duration"
                                       , msg.ResponseDurationMs);
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task SaveMessageAsync(Message message)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        await using var cmd = connection.CreateCommand();
        
        cmd.CommandText = $@"
                    INSERT INTO {TableName} (Timestamp, Content, Sender, WasFastPath, Provider, Model, ResponseDurationMs) 
                    VALUES (@ts, @content, @sender, @wasFastPath, @provider, @model, @duration);";
        cmd.Parameters.AddWithValue("@ts"
                                  , message.Timestamp.ToString("o")); // ISO 8601 format
        cmd.Parameters.AddWithValue("@content"
                                  , message.Content);
        cmd.Parameters.AddWithValue("@sender"
                                  , message.Sender);
        cmd.Parameters.AddWithValue("@wasFastPath"
                                  , message.WasFastPath ? 1 : 0);
        cmd.Parameters.AddWithValue("@provider"
                                  , (object?)message.Provider ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@model"
                                  , (object?)message.Model ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@duration"
                                  , message.ResponseDurationMs);
        await cmd.ExecuteNonQueryAsync();


        await transaction.CommitAsync();
    }

    public async Task<IEnumerable<Message>> LoadMessagesAsync()
    {
        var messages = new List<Message>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
                SELECT Id, Timestamp, Content, Sender, WasFastPath, Provider, Model, ResponseDurationMs 
                FROM {TableName} 
                ORDER BY Timestamp ASC;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new Message
                         {
                             Id                 = reader.GetInt32(0)
                           , Timestamp          = DateTime.Parse(reader.GetString(1))
                           , Content            = reader.GetString(2)
                           , Sender             = reader.GetString(3)
                           , WasFastPath        = reader.GetInt32(4) == 1
                           , Provider           = reader.IsDBNull(5) ? null : reader.GetString(5)
                           , Model              = reader.IsDBNull(6) ? null : reader.GetString(6)
                           , ResponseDurationMs = reader.GetDouble(7)
                         });
        }

        return messages;
    }

    public async Task<IEnumerable<Message>> GetMessagesSinceAsync(DateTime? since = null)
    {
        var messages = new List<Message>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();

        if (since.HasValue)
        {
            cmd.CommandText = $@"
                    SELECT Id, Timestamp, Content, Sender, WasFastPath, Provider, Model, ResponseDurationMs 
                    FROM {TableName} 
                    WHERE Timestamp >= @since 
                    ORDER BY Timestamp ASC;";
            cmd.Parameters.AddWithValue("@since"
                                       , since.Value.ToString("o"));
        }
        else
        {
            cmd.CommandText = $@"
                    SELECT Id, Timestamp, Content, Sender, WasFastPath, Provider, Model, ResponseDurationMs 
                    FROM {TableName} 
                    ORDER BY Timestamp ASC;";
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new Message
                         {
                             Id                 = reader.GetInt32(0)
                           , Timestamp          = DateTime.Parse(reader.GetString(1))
                           , Content            = reader.GetString(2)
                           , Sender             = reader.GetString(3)
                           , WasFastPath        = reader.GetInt32(4) == 1
                           , Provider           = reader.IsDBNull(5) ? null : reader.GetString(5)
                           , Model              = reader.IsDBNull(6) ? null : reader.GetString(6)
                           , ResponseDurationMs = reader.GetDouble(7)
                         });
        }

        return messages;
    }
    public async Task DeleteMessagesOlderThanAsync(DateTime cutoffUtc, [CallerMemberName] string caller = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        using var cmd = connection.CreateCommand();
        // Match the file's binding idiom: Timestamp is stored as TEXT via
        // DateTime.ToString("o"), so compare against the same lexicographic form.
        // Binding a raw DateTime here would let the provider pick a different
        // string format and silently break the < comparison.
        cmd.CommandText = $"DELETE FROM {TableName} WHERE Timestamp < @cutoff;";
        cmd.Parameters.AddWithValue("@cutoff"
                                  , cutoffUtc.ToString("o"));

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _loggingService.LogError(e, $"Calling method: {caller}",  Category.MemoryService);
        }
    }

    
    public async Task ClearMemoryAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM {TableName};";

        await cmd.ExecuteNonQueryAsync();
    }

}