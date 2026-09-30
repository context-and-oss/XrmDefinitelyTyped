namespace XrmDefinitelyTyped.Core.Domain;

public sealed record ControlModel(
    string Id,
    string? DataFieldName,
    string ClassId,
    IReadOnlyList<string>? TargetEntityTypes = null,
    bool CanBeNull = false,
    bool IsBusinessProcessFlow = false,
    Guid? QuickViewFormId = null,
    string? QuickViewEntityLogicalName = null);
