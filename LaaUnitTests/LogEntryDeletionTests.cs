using System.Text;
using LocalAIAssistant.Services.Logging;
using Serilog;

namespace LaaUnitTests;

public class LogEntryDeletionTests
{
    [Fact]
    public void ReadRecords_Assigns_Stable_Distinct_Identity_To_Duplicate_Lines()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "same\r\nsame\r\n", new UTF8Encoding(false));

            var records = NewestLogLineReader.ReadRecords(path, 2);

            Assert.Equal(2, records.Count);
            Assert.Equal(6, records[0].Offset);
            Assert.Equal(0, records[1].Offset);
            Assert.NotEqual(records[0].StorageId, records[1].StorageId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadRecordAtOffset_Reads_Only_Requested_Record()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "first\r\nsecond\r\n", new UTF8Encoding(false));

            var record = NewestLogLineReader.ReadRecordAtOffset(path, 7);

            Assert.NotNull(record);
            Assert.Equal("second", record.Text);
            Assert.Equal(7, record.Offset);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AddAsync_Persists_Only_Selected_Identity_Across_Store_Instances()
    {
        var logPath = Path.GetTempFileName();
        var deletionPath = $"{logPath}.deleted";
        try
        {
            File.WriteAllText(logPath, "same\nsame\n", new UTF8Encoding(false));
            var records = NewestLogLineReader.ReadRecords(logPath, 2);
            var store = new DeletedLogEntryStore(deletionPath);

            var deleted = await store.AddAsync(logPath, records[0].StorageId, records[0].Offset, records[0].Text);
            var persisted = await new DeletedLogEntryStore(deletionPath).GetAllAsync();

            Assert.True(deleted);
            Assert.Contains(records[0].StorageId, persisted);
            Assert.DoesNotContain(records[1].StorageId, persisted);
        }
        finally
        {
            File.Delete(logPath);
            if (File.Exists(deletionPath)) File.Delete(deletionPath);
        }
    }

    [Fact]
    public async Task AddAsync_Rejects_Stale_Or_Already_Deleted_Identity()
    {
        var logPath = Path.GetTempFileName();
        var deletionPath = $"{logPath}.deleted";
        try
        {
            File.WriteAllText(logPath, "entry\n", new UTF8Encoding(false));
            var record = Assert.Single(NewestLogLineReader.ReadRecords(logPath, 1));
            var store = new DeletedLogEntryStore(deletionPath);

            var first = await store.AddAsync(logPath, record.StorageId, record.Offset, record.Text);
            var duplicate = await store.AddAsync(logPath, record.StorageId, record.Offset, record.Text);
            var stale = await store.AddAsync(logPath, "missing", record.Offset, record.Text);

            Assert.True(first);
            Assert.False(duplicate);
            Assert.False(stale);
        }
        finally
        {
            File.Delete(logPath);
            if (File.Exists(deletionPath)) File.Delete(deletionPath);
        }
    }

    [Fact]
    public async Task LoggingService_Delete_Removes_Only_Selected_Entry_After_Restart()
    {
        var logPath = Path.GetTempFileName();
        var deletionPath = $"{logPath}.deleted";
        try
        {
            var lines = new[]
            {
                "{\"@t\":\"2026-09-18T10:00:00-07:00\",\"@m\":\"first\",\"@l\":\"Information\"}",
                "{\"@t\":\"2026-09-18T10:01:00-07:00\",\"@m\":\"second\",\"@l\":\"Information\"}"
            };
            File.WriteAllLines(logPath, lines, new UTF8Encoding(false));
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = new LoggingService(logger, logPath);
            var initial = await service.GetLogPageAsync(0, 200);
            var selected = Assert.Single(initial.Entries, entry => entry.Message == "second");

            var deleted = await service.DeleteLogEntryAsync(selected);
            var restarted = new LoggingService(logger, logPath);
            var remaining = await restarted.GetLogPageAsync(0, 200);

            Assert.True(deleted);
            Assert.Single(remaining.Entries);
            Assert.Equal("first", remaining.Entries[0].Message);
        }
        finally
        {
            File.Delete(logPath);
            if (File.Exists(deletionPath)) File.Delete(deletionPath);
        }
    }
}
