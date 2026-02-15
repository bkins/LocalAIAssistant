namespace LocalAIAssistant.Core.Environment;

public static class EnvironmentTheme
{
    public static Color PrimaryColor =>
            BuildEnvironment.Name.ToUpperInvariant() switch
            {
                    "DEV"  => Colors.DodgerBlue
                  , "QA"   => Colors.Orange
                  , "PROD" => Colors.Firebrick
                  , _      => Colors.Gray
            };
}
