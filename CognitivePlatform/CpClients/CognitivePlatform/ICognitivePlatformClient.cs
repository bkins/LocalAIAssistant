using System.Runtime.CompilerServices;
using CP.Client.Core.Common.ConnectivityToApi;
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
    
    public abstract Task<HttpResponseMessage> Ping(string callersCaller, [CallerFilePath] string caller = "", [CallerMemberName] string member = "");

    //public abstract Task<HttpResponseMessage> Ready();
}