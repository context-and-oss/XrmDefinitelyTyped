using XrmDefinitelyTyped.Core.Domain;
using XrmDefinitelyTyped.Core.Generation;
using XrmDefinitelyTyped.Core.Generation.Generators;
using XrmTyped.Shared.Domain;

namespace XrmTyped.Shared.Tests;

public sealed class FormGeneratorTests
{
    [Fact]
    public void Generate_UsesMetadataForNamedOptionSetsAndLookupTargets()
    {
        var form = new FormModel(
            Guid.NewGuid(),
            "ctx_sample",
            "Information",
            FormType.Main,
            [new TabModel("general", "General", [new SectionModel("details", "Details", [
                new ControlModel("ctx_status", "ctx_status", "3EF39988-22BB-4F0B-BBBE-64B5A3748AEE"),
                new ControlModel("ownerid", "ownerid", "270BD3DB-D9AF-4782-9025-509E298DEC0A"),
            ])])]);

        var entity = new EntityModel(
            10000,
            "ctx_Sample",
            "ctx_sample",
            "ctx_samples",
            "ctx_sampleid",
            [
                new AttributeModel("ctx_Status", "ctx_status", "ctx_sample_ctx_status", SpecialAttributeType.OptionSet, [], true, true, true),
                new AttributeModel("OwnerId", "ownerid", "string", SpecialAttributeType.EntityReference,
                    [new TargetEntitySet("systemuser", "systemusers"), new TargetEntitySet("team", "teams")], true, true, true),
            ],
            [],
            [new OptionSetModel("ctx_sample_ctx_status", [new OptionModel("Active", 1)])]);

        var file = Assert.Single(new FormGenerator().Generate([form], [entity]));

        Assert.Equal(Path.Combine("Form", "ctx_sample", "Main", "Information.d.ts"), file.Filename);
        Assert.Contains("Xrm.Attributes.OptionSetAttribute<ctx_sample_ctx_status>", file.Content, StringComparison.Ordinal);
        Assert.Contains("XDTForm.OptionSetControl<ctx_sample_ctx_status>", file.Content, StringComparison.Ordinal);
        Assert.Contains("XDTForm.LookupAttribute<\"systemuser\" | \"team\">", file.Content, StringComparison.Ordinal);
        Assert.Contains("XDTForm.LookupControl<\"systemuser\" | \"team\">", file.Content, StringComparison.Ordinal);
        Assert.Contains("interface general extends", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_UsesMultiSelectOptionSetType()
    {
        var form = new FormModel(
            Guid.NewGuid(),
            "ctx_sample",
            "Information",
            FormType.Main,
            [new TabModel("general", "General", [new SectionModel("details", "Details", [
                new ControlModel("ctx_tags", "ctx_tags", "4AA28AB7-9C13-4F57-A73D-AD894D048B5F"),
            ])])]);
        var entity = new EntityModel(
            10000, "ctx_Sample", "ctx_sample", "ctx_samples", "ctx_sampleid",
            [new AttributeModel("ctx_Tags", "ctx_tags", "ctx_sample_ctx_tags", SpecialAttributeType.MultiSelectOptionSet, [], true, true, true)],
            [],
            [new OptionSetModel("ctx_sample_ctx_tags", [new OptionModel("One", 1)])]);

        var file = Assert.Single(new FormGenerator().Generate([form], [entity]));

        Assert.Contains("Xrm.Attributes.MultiSelectOptionSetAttribute<ctx_sample_ctx_tags>", file.Content, StringComparison.Ordinal);
        Assert.Contains("XDTForm.MultiSelectOptionSetControl<ctx_sample_ctx_tags>", file.Content, StringComparison.Ordinal);
    }
    [Fact]
    public void Generate_SkipsUnsupportedFormsAndEmptyTabNames()
    {
        var main = new FormModel(
            Guid.NewGuid(), "ctx_sample", "Profile Web Form (Enhanced)", FormType.Main,
            [new TabModel(string.Empty, string.Empty, [new SectionModel(string.Empty, string.Empty, [])])]);
        var card = main with { Id = Guid.NewGuid(), Name = "Card", FormType = FormType.Card };

        var file = Assert.Single(new FormGenerator().Generate([main, card]));

        Assert.Equal(Path.Combine("Form", "ctx_sample", "Main", "ProfileWebFormEnhanced.d.ts"), file.Filename);
        Assert.DoesNotContain("interface  extends", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Tabs.>", file.Content, StringComparison.Ordinal);
    }
    [Fact]
    public void Generate_QuickViewUsesQuickViewFormShape()
    {
        var form = new FormModel(
            Guid.NewGuid(), "ctx_sample", "Information", FormType.Quick,
            [new TabModel("general", "General", [new SectionModel("details", "Details", [])])]);

        var file = Assert.Single(new FormGenerator().Generate([form]));

        Assert.Contains("extends XDTForm.QuickViewForm<Information.Tabs,Information.Controls>", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("interface Attributes extends", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("interface QuickViewForms extends", file.Content, StringComparison.Ordinal);
    }
    [Fact]
    public void Generate_RenamesDuplicateControlsAndPreservesNullability()
    {
        var duplicate = new ControlModel(
            "header_process_name", "name", "4273EDBD-AC1D-40D3-9FB2-095C621B552D",
            CanBeNull: true, IsBusinessProcessFlow: true);
        var form = new FormModel(
            Guid.NewGuid(), "account", "Information", FormType.Main,
            [new TabModel("general", "General", [new SectionModel("details", "Details", [duplicate, duplicate])])]);

        var file = Assert.Single(new FormGenerator().Generate([form]));

        Assert.Contains("get(name: \"header_process_name\"): Xrm.Controls.StringControl | null", file.Content, StringComparison.Ordinal);
        Assert.Contains("get(name: \"header_process_name_1\"): Xrm.Controls.StringControl | null", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_EmitsMappingsAndConfiguredFormIntersection()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var sharedControl = new ControlModel("name", "name", "4273EDBD-AC1D-40D3-9FB2-095C621B552D");
        var forms = new[]
        {
            new FormModel(firstId, "account", "One", FormType.Main,
                [new TabModel("general", "General", [new SectionModel("details", "Details", [sharedControl])])]),
            new FormModel(secondId, "account", "Two", FormType.Main,
                [new TabModel("general", "General", [new SectionModel("details", "Details", [sharedControl])])]),
        };
        var entity = new EntityModel(
            1, "Account", "account", "accounts", "accountid",
            [new AttributeModel("Name", "name", "string", SpecialAttributeType.Default, [], true, true, true)], [], []);
        var config = new XdtGenerationConfig(
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["SharedAccount"] = [firstId.ToString(), secondId.ToString()],
            },
            GenerateMappings: true);

        var files = new FormGenerator().Generate(forms, [entity], config);
        var intersection = Assert.Single(files, file => file.Filename == Path.Combine("Form", "_special", "SharedAccount.d.ts"));

        Assert.Contains("declare namespace Form._special", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface AttributeValueMap", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("\"name\": string;", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface ControlMap", intersection.Content, StringComparison.Ordinal);
    }
}
