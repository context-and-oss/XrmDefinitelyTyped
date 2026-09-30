using System.Text.RegularExpressions;

namespace XrmDefinitelyTyped.Core.Generation.Utilities;

internal static partial class TypeScriptIdentifier
{
    [GeneratedRegex(@"[^a-zA-Z0-9_$]")]
    private static partial Regex InvalidCharsRegex();

    public static string Sanitize(string name)
    {
        var result = InvalidCharsRegex().Replace(name, "_");
        return result.Length > 0 && !char.IsLetter(result[0]) ? "_" + result : result;
    }

    public static string RemoveInvalidCharacters(string name)
    {
        var result = InvalidCharsRegex().Replace(name, string.Empty);
        if (result.Length == 0)
            return "_EmptyString";

        return char.IsAsciiDigit(result[0]) ? $"_{result}" : result;
    }

    public static string ToPascalCase(string name)
    {
        var words = name.Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "_";
        return string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
