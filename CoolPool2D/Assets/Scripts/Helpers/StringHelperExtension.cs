public static class StringHelperExtension
{
    public static string Capitalize(this string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        s = s.Trim();
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}