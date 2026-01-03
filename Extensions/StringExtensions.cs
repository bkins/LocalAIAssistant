namespace LocalAIAssistant.Extensions;

public static class StringExtensions
{
    public static bool IsEmptyNullOrWhiteSpace (this string value)
    {
        return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace (value);
    }

    public static bool HasValue (this string value)
    {
        return IsEmptyNullOrWhiteSpace(value).Not();
    }

    public static bool HasNoValue(this string value)
    {
        return HasValue (value).Not();
    }

    public static bool DoesNotContain (this List<string> value
                                     , string            substring)
    {
        return value.Contains (substring).Not();
    }

    public static bool IsNotEqualTo (this string      source
                                   , string           other
                                   , StringComparison comparisonType)
    {
        return string.Equals(source
                           , other
                           , comparisonType).Not();
    }

    public static bool IsEqualTo (this string      source
                                , string           other
                                , StringComparison comparisonType)
    {
        return string.Equals(source
                           , other
                           , comparisonType);
    }

    public static bool IsInt (this string value)
    {
        return int.TryParse(value, out _);
    }

    public static bool IsNotInt (this string value)
    {
        return IsInt (value).Not();
    }
}
