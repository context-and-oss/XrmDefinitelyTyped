using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation.Generators;

namespace XrmTyped.Shared.Tests;

public sealed class OptionSetGeneratorTests
{
    [Fact]
    public void Generate_EmitsUnionDeclarationAndNamedConstObject()
    {
        var optionSet = new OptionSetModel("account_category", [new OptionModel("Preferred", 1), new OptionModel("Standard", 2)]);

        var files = new OptionSetGenerator().Generate([optionSet]);
        var declaration = Assert.Single(files, file => file.Filename == Path.Combine("_internal", "Enum", "account_category.d.ts"));
        var module = Assert.Single(files, file => file.Filename == "OptionSets.ts");

        Assert.Contains("declare type account_category = 1 | 2;", declaration.Content, StringComparison.Ordinal);
        Assert.Contains("export const account_category", module.Content, StringComparison.Ordinal);
        Assert.Contains("Preferred: 1,", module.Content, StringComparison.Ordinal);
        Assert.Contains("Standard: 2,", module.Content, StringComparison.Ordinal);
        Assert.Contains("export type account_category", module.Content, StringComparison.Ordinal);
        Assert.Contains("(typeof account_category)[keyof typeof account_category]", module.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithoutOptions_FallsBackToNumber()
    {
        var files = new OptionSetGenerator().Generate([new OptionSetModel("empty_set", [])]);
        var declaration = Assert.Single(files, file => file.Filename.EndsWith("empty_set.d.ts", StringComparison.Ordinal));
        var module = Assert.Single(files, file => file.Filename == "OptionSets.ts");

        Assert.Contains("declare type empty_set = number;", declaration.Content, StringComparison.Ordinal);
        Assert.Contains("export const empty_set = {", module.Content, StringComparison.Ordinal);
        Assert.Contains("export type empty_set =", module.Content, StringComparison.Ordinal);
        Assert.Contains("number;", module.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_DeduplicatesOptionSetsByNameAndOrdersThem()
    {
        var first = new OptionSetModel("b_set", [new OptionModel("One", 1)]);
        var second = new OptionSetModel("a_set", [new OptionModel("One", 1)]);

        var files = new OptionSetGenerator().Generate([first, second, first]);
        var declarationFiles = files.Where(file => file.Filename.EndsWith(".d.ts", StringComparison.Ordinal));

        Assert.Equal(
            [Path.Combine("_internal", "Enum", "a_set.d.ts"), Path.Combine("_internal", "Enum", "b_set.d.ts")],
            declarationFiles.Select(file => file.Filename));
        Assert.Single(files, file => file.Filename == "OptionSets.ts");
    }
}
