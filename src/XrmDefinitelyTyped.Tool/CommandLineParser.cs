using System.CommandLine;
using System.CommandLine.Parsing;
using XrmDefinitelyTyped.Tool.Configuration;

namespace XrmDefinitelyTyped.Tool;

internal static class CommandLineParser
{
    public static PartialConfig Parse(string[] args)
    {

        var outputOption = new Option<string>("--output-directory", "-o")
        {
            Description = "Output directory for generated files."
        };

        var solutionsOption = new Option<string[]>("--solutions", "-s")
        {
            Description = "Solution unique names.",
            AllowMultipleArgumentsPerToken = true,
            CustomParser = GetCommaSeparatedValue,
        };

        var entitiesOption = new Option<string[]>("--entities", "-e")
        {
            Description = "Entity logical names.",
            AllowMultipleArgumentsPerToken = true,
            CustomParser = GetCommaSeparatedValue,
        };

        var labelMappingsOption = new Option<Dictionary<string, string>>("--label-mappings", "-map")
        {
            Description = "Comma-separated list of mappings between unicodes and the mapped string. Example: \\u2714\\uFE0F: checkmark, \\u26D4\\uFE0F: stopsign",
            AllowMultipleArgumentsPerToken = true,
            CustomParser = result =>
            {
                var value = result.Tokens.Select(t => t.Value).ToArray();
                var dict = new Dictionary<string, string>(StringComparer.InvariantCulture);
                foreach (var mapping in value)
                {
                    var parts = mapping.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var key = System.Text.RegularExpressions.Regex.Unescape(parts[0].Trim());
                        var val = parts[1].Trim();
                        dict[key] = val;
                    }
                }

                return dict;
            },
        };

        var intersectOption = new Option<Dictionary<string, IReadOnlyList<string>>>("--intersect", "-i")
        {
            Description = "Comma-separated list of named semicolon-separated lists of form GUIDs to intersect. Example: SharedAccount:guid1;guid2",
            AllowMultipleArgumentsPerToken = true,
            CustomParser = result =>
            {
                var value = result.Tokens.Select(t => t.Value).ToArray();
                var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture);
                foreach (var entry in value)
                {
                    var parts = entry.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var interfaceName = parts[0].Trim();
                        var tableList = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        dict[interfaceName] = new List<string>(tableList).AsReadOnly();
                    }
                }

                return dict;
            },
        };

        // TODO: Change to include-inactive-forms
        var skipInactiveOption = new Option<bool?>("--skip-inactive-forms", "-sif")
        {
            Description = "Skip inactive forms.",
        };

        var singleFileOption = new Option<bool?>("--single-file", "-sf")
        {
            Description = "Emit a single output file.",
        };

        var customApisOption = new Option<bool?>("--generate-custom-apis", "-api")
        {
            Description = "Generate Custom API types (not yet implemented).",
        };

        var mappingsOption = new Option<bool?>("--generate-mappings", "-gm")
        {
            Description = "Generate AttributeValueMap and ControlMap interfaces for forms.",
        };

        var generateOption = new Option<string[]>("--generate", "-g")
        {
            Description = $"What to generate: '{GeneratorKinds.FormsValue}', '{GeneratorKinds.WebValue}' or both (default).",
            AllowMultipleArgumentsPerToken = true,
            CustomParser = GetCommaSeparatedValue,
        };

        var webNamespaceOption = new Option<string>("--web-namespace", "-wns")
        {
            Description = "Namespace for the generated web entity types.",
        };

        var root = new RootCommand("XrmDefinitelyTyped - generate TypeScript declarations from Dataverse forms.")
        {
            outputOption,
            solutionsOption,
            entitiesOption,
            intersectOption,
            skipInactiveOption,
            labelMappingsOption,
            singleFileOption,
            customApisOption,
            mappingsOption,
            generateOption,
            webNamespaceOption,
        };

        var result = root.Parse(args);

        if (result.Errors.Count > 0)
            throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Message)));

        return new PartialConfig(
            result.GetValue(outputOption),
            AsList(result.GetValue(solutionsOption)),
            AsList(result.GetValue(entitiesOption)),
            result.GetValue(labelMappingsOption),
            result.GetValue(intersectOption),
            result.GetValue(skipInactiveOption),
            result.GetValue(singleFileOption),
            result.GetValue(customApisOption),
            result.GetValue(mappingsOption),
            AsList(result.GetValue(generateOption)),
            result.GetValue(webNamespaceOption));
    }

    // An absent option parses to an empty array rather than null, and an explicit `-s ""` to [""].
    // Both must stay null so the appsettings.json fallback in Program.cs is not suppressed.
    private static List<string>? AsList(string[]? values)
    {
        if (values is null)
            return null;

        List<string> cleaned = [.. values.Select(value => value.Trim()).Where(value => value.Length > 0)];
        return cleaned.Count == 0 ? null : cleaned;
    }

    private static string[] GetCommaSeparatedValue(System.CommandLine.Parsing.ArgumentResult result)
    {
        var value = result.Tokens.Select(t => t.Value).ToArray();
        return value.Length == 1 && value[0].Contains(',', StringComparison.InvariantCulture)
            ? value[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : value;
    }
}
