using System.Runtime.CompilerServices;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.Environment.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

/// <summary>
/// Base class for all CognitivePlatform HTTP clients.
/// Renamed from ICognitivePlatformClient to accurately reflect that this
/// is an abstract base class, not an interface.
/// </summary>
public abstract class CognitivePlatformClientBase
{
    public IConnectivityReporter? Connectivity;

    public abstract Task<ConverseResponseDto> ConverseAsync( string            userMessage
                                                           , string            conversationId
                                                           , string            model
                                                           , CancellationToken ct = default );

    public abstract IAsyncEnumerable<string> ConverseStreamAsync( string            userMessage
                                                                , string            conversationId
                                                                , string            model
                                                                , CancellationToken ct = default );

    public abstract Task<SystemEnvironmentInfo> SystemEnvironmentAsync(CancellationToken ct = default);

    public abstract Task<HttpResponseMessage> Ping( string                    callersCaller
                                                  , [CallerFilePath]   string caller = ""
                                                  , [CallerMemberName] string member = "" );

    public abstract Task<GroqUsageDto> GetUsageAsync(CancellationToken ct = default);

    public abstract Task<List<ActionMetadataDto>> GetActionsAsync(CancellationToken ct = default);

    public abstract Task<VaultStatusDto> GetVaultStatusAsync(CancellationToken ct = default);
    public abstract Task<bool> UnlockVaultAsync(string pin, CancellationToken ct = default);
    public abstract Task<bool> SetupVaultAsync(string pin, CancellationToken ct = default);
    public abstract Task<bool> LockVaultAsync(CancellationToken ct = default);
}