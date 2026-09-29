namespace XrmDefinitelyTyped.Core.Generation.ViewModels;

internal sealed record FormViewModel(
    string EntityLogicalName,
    string FormTypeName,
    string FormName,
    bool IsQuickView,
    bool GenerateMappings,
    IReadOnlyList<TabViewModel> Tabs,
    IReadOnlyList<AttributeViewModel> Attributes,
    IReadOnlyList<ControlViewModel> Controls,
    IReadOnlyList<QuickViewFormViewModel> QuickViewForms);
