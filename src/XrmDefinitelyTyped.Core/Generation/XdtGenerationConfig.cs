namespace XrmDefinitelyTyped.Core.Generation;

public record XdtGenerationConfig(
    IReadOnlyDictionary<string, IReadOnlyList<string>> IntersectMapping,
    bool SingleFile = false,
    bool GenerateCustomApis = true,
    bool GenerateMappings = false);
