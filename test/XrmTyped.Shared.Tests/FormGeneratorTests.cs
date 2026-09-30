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
    public void Generate_EntityIntersectionsMatchFormsAndIgnoreUnmatchedForms()
    {
        var common = new ControlModel("name", "name", "4273EDBD-AC1D-40D3-9FB2-095C621B552D");
        var accountOnly = common with { Id = "accountonly", DataFieldName = "accountonly" };
        var forms = new[]
        {
            new FormModel(Guid.NewGuid(), "account", "Information", FormType.Main, [], [common, accountOnly]),
            new FormModel(Guid.NewGuid(), "account", "Unmatched", FormType.Main, [], [accountOnly]),
            new FormModel(Guid.NewGuid(), "contact", "Information", FormType.Main, [],
                [common, common with { Id = "header_process_name", CanBeNull = true, IsBusinessProcessFlow = true }]),
            new FormModel(Guid.NewGuid(), "account", "Quick", FormType.QuickCreate, [], [common]),
            new FormModel(Guid.NewGuid(), "contact", "Quick", FormType.QuickCreate, [], [common]),
        };
        var (account, contact) = CustomerEntities();
        var files = new FormGenerator().Generate(forms, [account, contact], CustomerConfig());
        var intersection = Assert.Single(files, file => file.Filename == Path.Combine("Form", "ICustomer", "Main", "Information.d.ts"));
        Assert.Contains("declare namespace Form.ICustomer.Main", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("\"name\": string | null;", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface ControlMap", intersection.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("accountonly", intersection.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("any", intersection.Content, StringComparison.Ordinal);
        Assert.Contains(files, file => file.Filename == Path.Combine("Form", "ICustomer", "QuickCreate", "Quick.d.ts"));
        Assert.DoesNotContain(files, file => file.Filename == Path.Combine("Form", "ICustomer", "Main", "Unmatched.d.ts"));
        Assert.Equal(7, files.Count);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("different-name")]
    [InlineData("different-type")]
    [InlineData("no-overlap")]
    [InlineData("incompatible-metadata")]
    public void Generate_SkipsFormIntersectionsWithoutCommonShape(string scenario)
    {
        var common = new ControlModel("name", "name", "4273EDBD-AC1D-40D3-9FB2-095C621B552D");
        var first = new FormModel(Guid.NewGuid(), "account", "Information", FormType.Main, [], [common]);
        var second = first with { Id = Guid.NewGuid(), EntityLogicalName = "contact" };
        second = scenario switch
        {
            "different-name" => second with { Name = "Other" },
            "different-type" => second with { FormType = FormType.QuickCreate },
            "no-overlap" => second with { AdditionalControls = [common with { Id = "other", DataFieldName = "other" }] },
            _ => second,
        };
        var (account, contact) = CustomerEntities();
        if (scenario == "incompatible-metadata")
            contact = contact with { Attributes = [contact.Attributes[0] with { TypeScriptType = "number" }] };
        FormModel[] forms = scenario == "missing" ? [first] : [first, second];
        var files = new FormGenerator().Generate(forms, [account, contact], CustomerConfig());
        Assert.Equal(forms.Length, files.Count);
        Assert.DoesNotContain(files, file => file.Filename.StartsWith(Path.Combine("Form", "ICustomer"), StringComparison.Ordinal));
    }

    private static (EntityModel Account, EntityModel Contact) CustomerEntities()
    {
        var attribute = new AttributeModel("Name", "name", "string", SpecialAttributeType.Default, [], true, true, true);
        var account = new EntityModel(1, "Account", "account", "accounts", "accountid", [attribute], [], []);
        return (account, account with { SchemaName = "Contact", LogicalName = "contact", EntitySetName = "contacts", PrimaryIdAttribute = "contactid" });
    }

    private static XdtGenerationConfig CustomerConfig() => new(
        new Dictionary<string, IReadOnlyList<string>> { ["ICustomer"] = ["account", "contact"] }, GenerateMappings: true);
}
