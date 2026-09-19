using System.Text;

namespace LocalAIAssistant.Services.Logging;

public static class NewestLogLineReader
{
    public static List<string> Read(string path, int count, CancellationToken cancellationToken = default)
    {
        var result = new List<string>(count);
        if (!File.Exists(path)) return result;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = new List<byte>();
        for (long position = stream.Length - 1; position >= 0 && result.Count < count; position--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Position = position;
            var value = stream.ReadByte();
            if (value == '\n')
            {
                if (bytes.Count == 0) continue;
                bytes.Reverse();
                result.Add(Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r'));
                bytes.Clear();
            }
            else bytes.Add((byte)value);
        }
        if (bytes.Count > 0 && result.Count < count)
        {
            bytes.Reverse();
            result.Add(Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r'));
        }
        return result;
    }
}
