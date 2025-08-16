using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Logging;
using Microsoft.Data.Sqlite;

namespace LocalAIAssistant.Services.AiMemory;

public class SqliteAiMemoryStore : IShortTermMemoryStore
{
    private readonly ILoggingService _loggingService;
    
    private readonly string _dbPath;
    private const    string TableName = "AiMemory";

    public SqliteAiMemoryStore(ILoggingService loggingService
                             , string          dbPath)
    {
        _loggingService = loggingService;
        
        // _dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory
        //                                , "AiMemory.db");
        _dbPath   = dbPath;
        
        EnsureDatabase();
        _loggingService.LogInformation($"Database created at: {_dbPath}");
    }

    private void EnsureDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {TableName} (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Sender TEXT NOT NULL
                );";
        command.ExecuteNonQuery();
    }

    public async Task SaveMessagesAsync(IEnumerable<Message> messages)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        foreach (var msg in messages)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                    INSERT INTO {TableName} (Timestamp, Content, Sender) 
                    VALUES (@ts, @content, @sender);";
            cmd.Parameters.AddWithValue("@ts"
                                      , msg.Timestamp.ToString("o")); // ISO 8601 format
            cmd.Parameters.AddWithValue("@content"
                                      , msg.Content);
            cmd.Parameters.AddWithValue("@sender"
                                      , msg.Sender);
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task SaveMessageAsync(Message message)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        await using var cmd = connection.CreateCommand();
        
        cmd.CommandText = $@"
                    INSERT INTO {TableName} (Timestamp, Content, Sender) 
                    VALUES (@ts, @content, @sender);";
        cmd.Parameters.AddWithValue("@ts"
                                  , message.Timestamp.ToString("o")); // ISO 8601 format
        cmd.Parameters.AddWithValue("@content"
                                  , message.Content);
        cmd.Parameters.AddWithValue("@sender"
                                  , message.Sender);
        await cmd.ExecuteNonQueryAsync();


        await transaction.CommitAsync();
    }

    public async Task<IEnumerable<Message>> LoadMessagesAsync()
    {
        var messages = new List<Message>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
                SELECT Id, Timestamp, Content, Sender 
                FROM {TableName} 
                ORDER BY Timestamp ASC;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new Message
                         {
                             Id        = reader.GetInt32(0)
                           , Timestamp = DateTime.Parse(reader.GetString(1))
                           , Content   = reader.GetString(2)
                           , Sender    = reader.GetString(3)
                         });
        }

        return messages;
    }

    public async Task<IEnumerable<Message>> GetMessagesSinceAsync(DateTime? since = null)
    {
        var messages = new List<Message>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();

        if (since.HasValue)
        {
            cmd.CommandText = $@"
                    SELECT Id, Timestamp, Content, Sender 
                    FROM {TableName} 
                    WHERE Timestamp >= @since 
                    ORDER BY Timestamp ASC;";
            cmd.Parameters.AddWithValue("@since"
                                      , since.Value.ToString("o"));
        }
        else
        {
            cmd.CommandText = $@"
                    SELECT Id, Timestamp, Content, Sender 
                    FROM {TableName} 
                    ORDER BY Timestamp ASC;";
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new Message
                         {
                             Id = reader.GetInt32(0)
                           , Timestamp = DateTime.Parse(reader.GetString(1))
                           , Content = reader.GetString(2)
                           , Sender = reader.GetString(3)
                         });
        }

        return messages;
    }

    
    public async Task ClearMemoryAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM {TableName};";

        await cmd.ExecuteNonQueryAsync();
    }

}