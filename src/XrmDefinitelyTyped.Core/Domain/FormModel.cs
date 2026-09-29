namespace XrmDefinitelyTyped.Core.Domain;

public sealed record FormModel(
    Guid Id,
    string EntityLogicalName,
    string Name,
    FormType FormType,
    IReadOnlyList<TabModel> Tabs,
    IReadOnlyList<ControlModel>? AdditionalControls = null,
    bool IsIntersection = false,
    string? MetadataEntityLogicalName = null);
