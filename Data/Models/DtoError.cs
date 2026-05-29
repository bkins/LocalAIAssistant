namespace LocalAIAssistant.Data.Models;

public class DtoError
{
    public string    Message        { get; init; } = string.Empty;
    public string?   StackTrace     { get; init; } = null;
    public string?   Source         { get; init; } = null;
    public string?   ExceptionType  { get; init; } = null;
    public DtoError? InnerException { get; init; } = null;
}