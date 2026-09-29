using Scriban;
using System.Reflection;
using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation.Utilities;
using XrmTyped.Shared.Generation.ViewModels;

namespace XrmTyped.Shared.Generation.Generators;

public sealed class OptionSetGenerator
{
    private const string ModuleFilename = "OptionSets.ts";

    private static readonly Template DeclarationTemplate = TemplateRenderer.Load(
        Assembly.GetExecutingAssembly(),
        "XrmTyped.Shared.Templates.optionset.sbn");

    private static readonly Template ModuleTemplate = TemplateRenderer.Load(
        Assembly.GetExecutingAssembly(),
        "XrmTyped.Shared.Templates.optionsets-module.sbn");

    public IReadOnlyList<GeneratedFile> Generate(IReadOnlyList<OptionSetModel> optionSets)
    {
        var viewModels = optionSets
            .DistinctBy(optionSet => optionSet.Name, StringComparer.Ordinal)
            .OrderBy(optionSet => optionSet.Name, StringComparer.Ordinal)
            .Select(BuildViewModel)
            .ToList();

        var declarations = viewModels.Select(GenerateOptionSetDeclaration);
        var module = new GeneratedFile(
            ModuleFilename,
            TemplateRenderer.Render(ModuleTemplate, new OptionSetsModuleViewModel(viewModels)));

        return [.. declarations, module];
    }

    private static OptionSetViewModel BuildViewModel(OptionSetModel optionSet) =>
        new(
            optionSet.Name,
            BuildUnion(optionSet),
            [.. optionSet.Options.Select(option => new OptionViewModel(option.Label, option.Value))]);

    private static GeneratedFile GenerateOptionSetDeclaration(OptionSetViewModel optionSet)
    {
        var content = TemplateRenderer.Render(DeclarationTemplate, optionSet);
        var filename = Path.Combine("_internal", "Enum", $"{optionSet.Name}.d.ts");
        return new GeneratedFile(filename, content);
    }

    private static string BuildUnion(OptionSetModel optionSet) =>
        optionSet.Options.Length == 0
            ? "number"
            : string.Join(" | ", optionSet.Options.Select(option => option.Value).Distinct().Order());
}
