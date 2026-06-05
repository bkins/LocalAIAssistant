using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.Services.Google;

public sealed class GoogleCalendarService : IGoogleCalendarService
{
    private const string AuthUrl  = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string Scope    = "https://www.googleapis.com/auth/calendar";

    private readonly HttpClient _http;

    // In-memory access token cache — cleared on disconnect.
    private string?   _cachedAccessToken;
    private DateTime  _accessTokenExpiry = DateTime.MinValue;

    public string ClientId =>
        Preferences.Default.Get(StringConsts.GoogleCalendarClientIdPrefKey, string.Empty);

    public bool HasToken
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ClientId)) return false;
            try
            {
                // SecureStorage.GetAsync is async; check the synchronous key-exists path via
                // a try/get pattern — if SecureStorage throws (e.g. key missing) return false.
                var token = SecureStorage.Default.GetAsync(StringConsts.GoogleCalendarRefreshTokenKey)
                                                 .GetAwaiter().GetResult();
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }
    }

    public GoogleCalendarService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> ConnectAsync()
    {
        var clientId = ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            return false;

#if WINDOWS
        return await ConnectWindowsAsync(clientId);
#elif ANDROID
        return await ConnectAndroidAsync(clientId);
#else
        return false;
#endif
    }

    public async Task DisconnectAsync()
    {
        SecureStorage.Default.Remove(StringConsts.GoogleCalendarRefreshTokenKey);
        SecureStorage.Default.Remove(StringConsts.GoogleCalendarAccessTokenKey);
        SecureStorage.Default.Remove(StringConsts.GoogleCalendarTokenExpiryKey);
        _cachedAccessToken = null;
        _accessTokenExpiry = DateTime.MinValue;

        await Task.CompletedTask;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_cachedAccessToken is not null && DateTime.UtcNow < _accessTokenExpiry)
            return _cachedAccessToken;

        var refreshToken = await SecureStorage.Default.GetAsync(StringConsts.GoogleCalendarRefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken)) return null;

        return await RefreshAccessTokenAsync(refreshToken);
    }

    // ── PKCE helpers ──────────────────────────────────────────────────────────

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var bytes     = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
                  .TrimEnd('=')
                  .Replace('+', '-')
                  .Replace('/', '_');

    // ── Token exchange ────────────────────────────────────────────────────────

    private async Task<bool> ExchangeCodeForTokensAsync( string clientId
                                                        , string code
                                                        , string verifier
                                                        , string redirectUri )
    {
        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string,string>("grant_type",    "authorization_code")
          , new KeyValuePair<string,string>("code",          code)
          , new KeyValuePair<string,string>("client_id",     clientId)
          , new KeyValuePair<string,string>("redirect_uri",  redirectUri)
          , new KeyValuePair<string,string>("code_verifier", verifier)
        ]);

        var response = await _http.PostAsync(TokenUrl, form);
        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadAsStringAsync();
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("refresh_token", out var rtProp)) return false;

        var refreshToken = rtProp.GetString();
        if (string.IsNullOrEmpty(refreshToken)) return false;

        await SecureStorage.Default.SetAsync(StringConsts.GoogleCalendarRefreshTokenKey, refreshToken);

        if (root.TryGetProperty("access_token", out var atProp))
        {
            var accessToken = atProp.GetString() ?? string.Empty;
            var expiresIn   = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 3600;
            _cachedAccessToken = accessToken;
            _accessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            await SecureStorage.Default.SetAsync(StringConsts.GoogleCalendarAccessTokenKey, accessToken);
            await SecureStorage.Default.SetAsync(StringConsts.GoogleCalendarTokenExpiryKey
                                                , _accessTokenExpiry.ToString("O"));
        }

        return true;
    }

    private async Task<string?> RefreshAccessTokenAsync(string refreshToken)
    {
        var clientId = ClientId;
        if (string.IsNullOrWhiteSpace(clientId)) return null;

        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string,string>("grant_type",    "refresh_token")
          , new KeyValuePair<string,string>("refresh_token", refreshToken)
          , new KeyValuePair<string,string>("client_id",     clientId)
        ]);

        var response = await _http.PostAsync(TokenUrl, form);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var atProp)) return null;

        var accessToken = atProp.GetString();
        var expiresIn   = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 3600;

        _cachedAccessToken = accessToken;
        _accessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

        return accessToken;
    }

#if WINDOWS
    // ── Windows: browser + local HttpListener ─────────────────────────────────

    private async Task<bool> ConnectWindowsAsync(string clientId)
    {
        const int port = 8756;
        var redirectUri   = $"http://localhost:{port}/oauth-callback";
        var codeVerifier  = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var authUrl = $"{AuthUrl}"
                    + $"?client_id={Uri.EscapeDataString(clientId)}"
                    + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                    + $"&response_type=code"
                    + $"&scope={Uri.EscapeDataString(Scope)}"
                    + $"&code_challenge={codeChallenge}"
                    + $"&code_challenge_method=S256"
                    + $"&access_type=offline"
                    + $"&prompt=consent";

        using var listener = new System.Net.HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/oauth-callback/");
        listener.Start();

        await Launcher.Default.OpenAsync(new Uri(authUrl));

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        string? code = null;

        try
        {
            var context = await listener.GetContextAsync().WaitAsync(cts.Token);

            code = context.Request.QueryString["code"];

            var htmlResponse = string.IsNullOrEmpty(code)
                ? "<html><body>Authorization failed. You may close this window.</body></html>"
                : "<html><body>Google Calendar connected! You may close this window.</body></html>";

            var buffer = Encoding.UTF8.GetBytes(htmlResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType     = "text/html";
            await context.Response.OutputStream.WriteAsync(buffer, cts.Token);
            context.Response.Close();
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            listener.Stop();
        }

        if (string.IsNullOrEmpty(code)) return false;

        return await ExchangeCodeForTokensAsync(clientId, code, codeVerifier, redirectUri);
    }
#endif

#if ANDROID
    // ── Android: WebAuthenticator ─────────────────────────────────────────────

    private async Task<bool> ConnectAndroidAsync(string clientId)
    {
        const string callbackScheme = "ai.cognitiveplatform";
        var redirectUri  = $"{callbackScheme}://oauth-callback";
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var authUri = new Uri(
            $"{AuthUrl}"
          + $"?client_id={Uri.EscapeDataString(clientId)}"
          + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
          + $"&response_type=code"
          + $"&scope={Uri.EscapeDataString(Scope)}"
          + $"&code_challenge={codeChallenge}"
          + $"&code_challenge_method=S256"
          + $"&access_type=offline"
          + $"&prompt=consent");

        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(authUri, new Uri(redirectUri));
            if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
                return false;

            return await ExchangeCodeForTokensAsync(clientId, code, codeVerifier, redirectUri);
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
#endif
}
