using LocalAIAssistant.Core.Data;
using Microsoft.Data.Sqlite;

namespace LaaUnitTests;

// BACK-08: every SQLite connection-string build site in LAA's data layer routes
// through SqliteConnectionStrings.ForDataSource. The helper must escape any path
// with connection-string metacharacters (spaces, quotes, semicolons, equals)
// such that SqliteConnectionStringBuilder round-trips DataSource back to the
// original value — not a leaked extra-token interpretation of the path.
public class SqliteConnectionStringsTests
{
    [Theory]
    [InlineData(@"C:\Users\benho\AppData\Local\AiMemory.db")]
    [InlineData(@"C:\Program Files\My App\AiMemory.db")]              // space
    [InlineData(@"C:\db with ""quote"".db")]                          // double quote
    [InlineData(@"C:\db with 'apos'.db")]                             // single quote
    [InlineData(@"C:\db;with;semicolons.db")]                         // semicolons
    [InlineData(@"C:\db=with=equals.db")]                             // equals
    [InlineData(@"/tmp/some path/AiMemory.db")]                       // POSIX-style with space
    [InlineData(":memory:")]                                          // SQLite in-memory pseudo-path
    public void ForDataSource_RoundTrips_DataSource_ForEdgeCharacterPaths(string path)
    {
        var connectionString = SqliteConnectionStrings.ForDataSource(path);

        var roundTripped = new SqliteConnectionStringBuilder(connectionString);

        Assert.Equal(path, roundTripped.DataSource);
    }

    [Fact]
    public void ForDataSource_DoesNotLeakExtraTokens_FromMaliciousPath()
    {
        // A path with a semicolon and an embedded "Mode=ReadOnly" must not be
        // interpreted as setting the connection's Mode. The builder must escape
        // the path so the whole string ends up as DataSource.
        const string trickyPath = @"C:\evil;Mode=ReadOnly;.db";

        var connectionString = SqliteConnectionStrings.ForDataSource(trickyPath);

        var roundTripped = new SqliteConnectionStringBuilder(connectionString);

        Assert.Equal(trickyPath, roundTripped.DataSource);
        // No leaked Mode token from the path.
        Assert.Equal(SqliteOpenMode.ReadWriteCreate, roundTripped.Mode);
    }
}
