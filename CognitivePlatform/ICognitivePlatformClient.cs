using System.Runtime.CompilerServices;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.CognitivePlatform;

public interface ICognitivePlatformClient
{
    Task<ConverseResponseDto> ConverseAsync (string userMessage
                                           , string conversationId
                                           , string model);

    IAsyncEnumerable<string> ConverseStreamAsync (string                                     userMessage
                                                , string                                     conversationId
                                                , string                                     model
                                                , [EnumeratorCancellation] CancellationToken ct = default);

}