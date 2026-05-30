using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services.FileSync;

/// <summary>
/// Background HTTP listener that exposes selected device folders to the CP API over the local network.
///
/// Supported endpoints:
/// <list type="bullet">
///   <item><c>GET  /files/ping</c> — connectivity check; returns <c>{"status":"ok","deviceName":"..."}</c></item>
///   <item><c>GET  /files/list?path=&lt;encoded&gt;</c> — JSON array of <see cref="FileEntryDto"/></item>
///   <item><c>GET  /files/download?path=&lt;encoded&gt;</c> — raw file bytes (application/octet-stream)</item>
///   <item><c>POST /files/upload?path=&lt;encoded&gt;</c> — write request body to the specified file path</item>
///   <item><c>DELETE /files/delete?path=&lt;encoded&gt;</c> — delete the specified file</item>
/// </list>
///
/// <para><b>Security:</b> every request must include <c>X-CP-Key: &lt;SharedSecret&gt;</c>.
/// All <c>path</c> parameters are validated against <see cref="FileGatewayConfig.AllowedPaths"/> before any
/// file-system access; requests outside the allowlist receive 403.</para>
///
/// <para><b>Platform:</b> active on Android and Windows only; a no-op log entry is written on other targets.</para>
///
/// <para><b>Manual test steps (Postman / curl):</b></para>
/// <code>
/// # Ping
/// curl -H "X-CP-Key: &lt;secret&gt;" http://&lt;device-ip&gt;:5051/files/ping
///
/// # List directory
/// curl -H "X-CP-Key: &lt;secret&gt;" \
///      "http://&lt;device-ip&gt;:5051/files/list?path=%2Fstorage%2Femulated%2F0%2FDocuments"
///
/// # Download a file
/// curl -H "X-CP-Key: &lt;secret&gt;" \
///      "http://&lt;device-ip&gt;:5051/files/download?path=%2Fstorage%2Femulated%2F0%2FDocuments%2Fnote.txt" \
///      -o note.txt
///
/// # Upload a file
/// curl -X POST -H "X-CP-Key: &lt;secret&gt;" \
///      --data-binary @note.txt \
///      "http://&lt;device-ip&gt;:5051/files/upload?path=%2Fstorage%2Femulated%2F0%2FDocuments%2Fnote.txt"
///
/// # Delete a file
/// curl -X DELETE -H "X-CP-Key: &lt;secret&gt;" \
///      "http://&lt;device-ip&gt;:5051/files/delete?path=%2Fstorage%2Femulated%2F0%2FDocuments%2Fnote.txt"
/// </code>
/// </summary>
public sealed class FileGatewayService : BackgroundService
{
    private const string CpKeyHeader = "X-CP-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly FileGatewayConfig             _config;
    private readonly ILogger<FileGatewayService>   _logger;

    public FileGatewayService( IOptions<FileGatewayConfig>   config
                             , ILogger<FileGatewayService>    logger )
    {
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!IsSupportedPlatform())
        {
            _logger.LogInformation("FileGatewayService: skipped — Android and Windows only");
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.SharedSecret))
        {
            _logger.LogWarning( "FileGatewayService: SharedSecret is not configured; "
                              + "set FileGateway:SharedSecret in appsettings.json and restart the app" );
            return;
        }

        var prefix   = $"http://+:{_config.Port}/files/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
            _logger.LogInformation("FileGatewayService: listening on {Prefix}", prefix);

            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (!ct.IsCancellationRequested)
                        _logger.LogWarning(ex, "FileGatewayService: error accepting connection");
                    continue;
                }

                _ = Task.Run(() => HandleRequestAsync(context, ct), CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FileGatewayService: fatal error starting listener on {Prefix}", prefix);
        }
        finally
        {
            if (listener.IsListening)
                listener.Stop();
            listener.Close();
            _logger.LogInformation("FileGatewayService: stopped");
        }
    }

    // -----------------------------------------------------------------------
    // Request dispatch
    // -----------------------------------------------------------------------

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request  = context.Request;
        var response = context.Response;

        try
        {
            if (!ValidateKey(request))
            {
                await WriteErrorAsync(response, 403, "Forbidden: invalid or missing X-CP-Key", ct);
                return;
            }

            var route = (request.Url?.AbsolutePath ?? string.Empty).TrimStart('/');

            if (route == "files/ping")
                await HandlePingAsync(response, ct);
            else if (route == "files/list" && request.HttpMethod == "GET")
                await HandleListAsync(request, response, ct);
            else if (route == "files/download" && request.HttpMethod == "GET")
                await HandleDownloadAsync(request, response, ct);
            else if (route == "files/upload" && request.HttpMethod == "POST")
                await HandleUploadAsync(request, response, ct);
            else if (route == "files/delete" && request.HttpMethod == "DELETE")
                await HandleDeleteAsync(request, response, ct);
            else
                await WriteErrorAsync(response, 404, $"Not found: {route}", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FileGatewayService: unhandled error for {Method} {Url}"
                           , request.HttpMethod
                           , request.Url);
            try
            {
                await WriteErrorAsync(response, 500, "Internal server error", ct);
            }
            catch
            {
                // Response may already be partially written; swallow secondary error
            }
        }
        finally
        {
            response.Close();
        }
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private async Task HandlePingAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var deviceName = DeviceInfo.Current.Name;
        var payload    = JsonSerializer.Serialize(new { status = "ok", deviceName }, JsonOptions);
        await WriteJsonAsync(response, 200, payload, ct);
    }

    private async Task HandleListAsync( HttpListenerRequest  request
                                      , HttpListenerResponse response
                                      , CancellationToken    ct )
    {
        var resolvedPath = ResolvePathParam(request);
        if (resolvedPath is null || !IsPathAllowed(resolvedPath))
        {
            await WriteErrorAsync(response, 403, "Forbidden: path is outside the allowed list", ct);
            return;
        }

        if (!Directory.Exists(resolvedPath))
        {
            await WriteErrorAsync(response, 404, $"Directory not found: {resolvedPath}", ct);
            return;
        }

        var entries = Directory.EnumerateFileSystemEntries(resolvedPath)
                               .Select(entryFullPath => BuildEntry(entryFullPath))
                               .ToList();

        var payload = JsonSerializer.Serialize(entries, JsonOptions);
        await WriteJsonAsync(response, 200, payload, ct);
    }

    private async Task HandleDownloadAsync( HttpListenerRequest  request
                                          , HttpListenerResponse response
                                          , CancellationToken    ct )
    {
        var resolvedPath = ResolvePathParam(request);
        if (resolvedPath is null || !IsPathAllowed(resolvedPath))
        {
            await WriteErrorAsync(response, 403, "Forbidden: path is outside the allowed list", ct);
            return;
        }

        if (!File.Exists(resolvedPath))
        {
            await WriteErrorAsync(response, 404, $"File not found: {resolvedPath}", ct);
            return;
        }

        response.StatusCode  = 200;
        response.ContentType = "application/octet-stream";

        await using var fileStream = File.OpenRead(resolvedPath);
        response.ContentLength64 = fileStream.Length;
        await fileStream.CopyToAsync(response.OutputStream, ct);
    }

    private async Task HandleUploadAsync( HttpListenerRequest  request
                                        , HttpListenerResponse response
                                        , CancellationToken    ct )
    {
        var resolvedPath = ResolvePathParam(request);
        if (resolvedPath is null || !IsPathAllowed(resolvedPath))
        {
            await WriteErrorAsync(response, 403, "Forbidden: path is outside the allowed list", ct);
            return;
        }

        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = File.Create(resolvedPath);
        await request.InputStream.CopyToAsync(fileStream, ct);

        await WriteJsonAsync(response, 200, """{"status":"ok"}""", ct);
    }

    private async Task HandleDeleteAsync( HttpListenerRequest  request
                                        , HttpListenerResponse response
                                        , CancellationToken    ct )
    {
        var resolvedPath = ResolvePathParam(request);
        if (resolvedPath is null || !IsPathAllowed(resolvedPath))
        {
            await WriteErrorAsync(response, 403, "Forbidden: path is outside the allowed list", ct);
            return;
        }

        if (!File.Exists(resolvedPath))
        {
            await WriteErrorAsync(response, 404, $"File not found: {resolvedPath}", ct);
            return;
        }

        File.Delete(resolvedPath);
        await WriteJsonAsync(response, 200, """{"status":"ok"}""", ct);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private bool ValidateKey(HttpListenerRequest request)
        => string.Equals(request.Headers[CpKeyHeader], _config.SharedSecret, StringComparison.Ordinal);

    /// <summary>
    /// Decodes the <c>path</c> query parameter, blocks <c>..</c> traversal attempts,
    /// and returns the fully-resolved absolute path — or <c>null</c> if the input is
    /// absent or invalid.
    /// </summary>
    private static string? ResolvePathParam(HttpListenerRequest request)
    {
        var raw = request.QueryString["path"];
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (raw.Contains(".."))
            return null;

        try
        {
            return Path.GetFullPath(raw);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns <c>true</c> only when <paramref name="resolvedPath"/> is the same as,
    /// or a child of, one of the configured <see cref="FileGatewayConfig.AllowedPaths"/>.
    /// Comparison is case-insensitive on Windows (case-sensitive on Android).
    /// </summary>
    private bool IsPathAllowed(string resolvedPath)
    {
        if (_config.AllowedPaths is not { Length: > 0 })
            return false;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return _config.AllowedPaths.Any(allowedPath =>
        {
            try
            {
                var normalized = Path.GetFullPath(allowedPath);
                var boundary   = normalized.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return resolvedPath.Equals(normalized, comparison)
                    || resolvedPath.StartsWith(boundary, comparison);
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    private static FileEntryDto BuildEntry(string fullPath)
    {
        var isDirectory = Directory.Exists(fullPath);

        if (isDirectory)
        {
            var dir = new DirectoryInfo(fullPath);
            return new FileEntryDto
                   {
                       RelativePath = dir.Name
                     , SizeBytes    = 0L
                     , LastModified = new DateTimeOffset(dir.LastWriteTimeUtc, TimeSpan.Zero)
                     , IsDirectory  = true
                   };
        }

        var file = new FileInfo(fullPath);
        return new FileEntryDto
               {
                   RelativePath = file.Name
                 , SizeBytes    = file.Length
                 , LastModified = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero)
                 , IsDirectory  = false
               };
    }

    private static async Task WriteJsonAsync( HttpListenerResponse response
                                            , int                  statusCode
                                            , string               json
                                            , CancellationToken    ct )
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.StatusCode      = statusCode;
        response.ContentType     = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, ct);
    }

    private static Task WriteErrorAsync( HttpListenerResponse response
                                       , int                  statusCode
                                       , string               message
                                       , CancellationToken    ct )
        => WriteJsonAsync(response, statusCode, JsonSerializer.Serialize(new { error = message }), ct);

    private static bool IsSupportedPlatform()
    {
#if ANDROID || WINDOWS
        return true;
#else
        return false;
#endif
    }

    // -----------------------------------------------------------------------
    // DTOs
    // -----------------------------------------------------------------------

    private sealed record FileEntryDto
    {
        public string         RelativePath { get; init; } = string.Empty;
        public long           SizeBytes    { get; init; }
        public DateTimeOffset LastModified { get; init; }
        public bool           IsDirectory  { get; init; }
    }
}
