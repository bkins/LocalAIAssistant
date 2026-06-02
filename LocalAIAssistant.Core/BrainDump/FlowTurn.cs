namespace LocalAIAssistant.Core.BrainDump;

public enum FlowAction
{
    Continue
  , CreateTask   // Caller should create TaskTitle via NL then continue
  , Done
}

public record FlowTurn(
    string     Message
  , FlowAction Action    = FlowAction.Continue
  , string?    TaskTitle = null
);
