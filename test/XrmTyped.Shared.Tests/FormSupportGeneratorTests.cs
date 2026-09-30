using XrmDefinitelyTyped.Core.Generation.Generators;

namespace XrmTyped.Shared.Tests;

public sealed class FormSupportGeneratorTests
{
    [Fact]
    public void Generate_EmitsThinAugmentationOverTypesXrm()
    {
        var file = new FormSupportGenerator().Generate();

        Assert.Equal(Path.Combine("_internal", "XrmDefinitelyTyped.d.ts"), file.Filename);
        Assert.Contains("/// <reference types=\"xrm\" />", file.Content, StringComparison.Ordinal);
        Assert.Contains("import \"@delegateas/xrmquery\";", file.Content, StringComparison.Ordinal);
        Assert.Contains("extends Xrm.FormContext", file.Content, StringComparison.Ordinal);
        Assert.Contains("extends Xrm.Attributes.LookupAttribute", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("declare namespace Xrm", file.Content, StringComparison.Ordinal);
    }
}
