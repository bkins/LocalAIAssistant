using System.Runtime.CompilerServices;
using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.Environment.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public abstract class ICognitivePlatformClient
{
    public IConnectivityReporter? Connectivity;

    public abstract Task<ConverseResponseDto> ConverseAsync (string userMessage
                                                           , string conversationId
                                                           , string model);

    public abstract IAsyncEnumerable<string> ConverseStreamAsync (string            userMessage
                                                                , string            conversationId
                                                                , string            model
                                                                , CancellationToken ct = default);

    public abstract Task<SystemEnvironmentInfo> SystemEnvironmentAsync (CancellationToken ct = default);
    
    public abstract Task Ping();

    public abstract Task<HttpResponseMessage> Ready();
}