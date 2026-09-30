namespace XrmDefinitelyTyped.Core.Generation.ViewModels;

internal sealed record TabViewModel(
    string Identifier,
    string Name,
    IReadOnlyList<SectionViewModel> Sections);
