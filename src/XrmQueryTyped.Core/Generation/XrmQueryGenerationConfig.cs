namespace XrmQueryTyped.Core.Generation;

public sealed record XrmQueryGenerationConfig(
    string Namespace = "XDT",
    bool SingleFile = false,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? IntersectMapping = null);
