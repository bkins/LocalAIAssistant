using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace LocalAIAssistant;

public class FileOptionsSource<T> : IOptionsChangeTokenSource<T>
    where T : class, new()
{
    private readonly string _filePath;

    public FileOptionsSource(string filePath)
    {
        _filePath = filePath;
    }

    public string Name => Options.DefaultName;

    public IChangeToken GetChangeToken()
    {
        return new PhysicalFileProvider(Path.GetDirectoryName(_filePath)!).Watch(Path.GetFileName(_filePath));
    }
}