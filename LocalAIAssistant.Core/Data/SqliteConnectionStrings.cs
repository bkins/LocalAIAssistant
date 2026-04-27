using Microsoft.Data.Sqlite;

namespace LocalAIAssistant.Core.Data;

// BACK-08 (mirrors BACK-05): single helper for every SQLite connection-string
// build site in LAA. Routes through SqliteConnectionStringBuilder so paths with
// quotes, spaces, semicolons, equals signs, etc. are escaped correctly. No raw
// string interpolation of a path into a connection string anywhere in the
// data layer.
public static class SqliteConnectionStrings
{
    public static string ForDataSource(string dbPath) =>
        new SqliteConnectionStringBuilder
        {
                DataSource = dbPath
        }.ToString();
}
