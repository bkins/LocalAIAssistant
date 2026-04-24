namespace LocalAIAssistant.Core.Time;

public static class TimestampConversion
{
    // Kind=Unspecified is treated as UTC — the project convention is that
    // persisted timestamps round-trip through .ToString("o") / DateTime.Parse
    // and may lose Kind along the way; BUG-13 confirmed storage is UTC.
    public static DateTime ToLocalSafe(DateTime value) =>
            value.Kind switch
            {
                    DateTimeKind.Local       => value
                  , DateTimeKind.Utc         => value.ToLocalTime()
                  , DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
                  , _                        => value
            };
}
