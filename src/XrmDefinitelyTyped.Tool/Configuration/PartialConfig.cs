namespace XrmDefinitelyTyped.Tool.Configuration;

public record PartialConfig(
    string? OutputDirectory,
    List<string>? Solutions,
    List<string>? Entities,
    Dictionary<string, string>? LabelMappings,
    Dictionary<string, IReadOnlyList<string>>? IntersectMapping,
    bool? SkipInactiveForms,
    bool? SingleFile,
    bool? GenerateCustomApis,
    bool? GenerateMappings,
    List<string>? Generate,
    string? WebNamespace);
