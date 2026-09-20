using System.Text.Json.Serialization;

namespace LocalAIAssistant.Knowledge.Tasks.Models;

/// <summary>
/// Client-side mirror of CognitivePlatform.Api.Domains.Tasks.TaskPriority.
/// Values must stay in sync with the API enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriorityDto
{
    Low      = 0
  , Normal   = 1
  , High     = 2
  , Critical = 3
}
