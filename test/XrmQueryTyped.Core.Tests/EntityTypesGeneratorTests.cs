using XrmTyped.Shared.Generation;
using XrmQueryTyped.Core.Generation;

namespace XrmQueryTyped.Core.Tests;

public sealed class EntityTypesGeneratorTests
{
    private static readonly XrmQueryGenerationConfig DefaultConfig = new();

    [Theory]
    [InlineData("account")]
    [InlineData("contact")]
    public void Generate_WritesOneFilePerEntityMatchingFixture(string logicalName)
    {
        var files = Generate(DefaultConfig);

        var file = files.Single(generated => generated.Filename == Path.Combine("Web", $"{logicalName}.d.ts"));
        Assert.Equal(Fixture($"{logicalName}.expected.d.ts"), Normalize(file.Content));
    }

    [Fact]
    public void Generate_WithSingleFile_ConcatenatesIntoWebEntities()
    {
        var files = Generate(DefaultConfig with { SingleFile = true });

        var file = Assert.Single(files);
        Assert.Equal(Path.Combine("Web", "WebEntities.d.ts"), file.Filename);
        Assert.Contains("interface Account_Select {", file.Content, StringComparison.Ordinal);
        Assert.Contains("interface Contact_Select {", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithoutNamespace_EmitsInterfacesAtGlobalScope()
    {
        var files = Generate(DefaultConfig with { Namespace = "" });

        var content = files[0].Content;
        Assert.DoesNotContain("declare namespace", content, StringComparison.Ordinal);
        Assert.Contains("interface Account_Fixed {\n  accountid: string;\n}", Normalize(content), StringComparison.Ordinal);
        Assert.Contains("accounts: WebMappingRetrieve<Account_Select,", Normalize(content), StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithoutEntitySetName_OmitsGlobalMergeBlocks()
    {
        var entity = TestEntityModels.Account() with { EntitySetName = null };

        var files = new EntityTypesGenerator().Generate([entity], DefaultConfig);

        Assert.DoesNotContain("WebEntitiesRetrieve", files[0].Content, StringComparison.Ordinal);
        Assert.Contains("interface Account_Fixed {", files[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithWideEntity_DoesNotHitTemplateLoopLimit()
    {
        var files = new EntityTypesGenerator().Generate([TestEntityModels.Wide(400)], DefaultConfig);

        var file = files.Single(generated => generated.Filename == Path.Combine("Web", "wide.d.ts"));
        Assert.Contains("field400: string;", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithOutputLargerThanOneMebibyte_IsNotTruncated()
    {
        var files = new EntityTypesGenerator().Generate([TestEntityModels.Wide(8000)], DefaultConfig);

        var content = files.Single(generated => generated.Filename == Path.Combine("Web", "wide.d.ts")).Content;
        Assert.True(content.Length > 1024 * 1024, $"Expected more than 1 MiB of output, got {content.Length} chars.");
        Assert.DoesNotContain("...", content, StringComparison.Ordinal);
        Assert.Contains("field8000: string;", content, StringComparison.Ordinal);
    }


    [Fact]
    public void Generate_EntityIntersectionEmitsSharedQueryContractWithoutEntitySet()
    {
        var attribute = new XrmTyped.Shared.Domain.AttributeModel("Name", "name", "string",
            XrmTyped.Shared.Domain.SpecialAttributeType.Default, [], true, true, true);
        var account = TestEntityModels.Account() with { Attributes = [attribute], Relationships = [] };
        var contact = TestEntityModels.Contact() with { Attributes = [attribute with { Updateable = false }], Relationships = [] };
        var config = DefaultConfig with
        {
            IntersectMapping = new Dictionary<string, IReadOnlyList<string>> { ["ICustomer"] = ["account", "contact"] },
        };

        var files = new EntityTypesGenerator().Generate([account, contact], config);
        var intersection = Assert.Single(files, file => file.Filename == Path.Combine("Web", "ICustomer.d.ts"));
        Assert.Contains("interface ICustomer_Select", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("WebAttribute<ICustomer_Select, { name: string | null }, object>", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface ICustomer_Filter", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface ICustomer_Update {\n  }", Normalize(intersection.Content), StringComparison.Ordinal);
        Assert.DoesNotContain("accountid", intersection.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("contactid", intersection.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("WebEntitiesRetrieve", intersection.Content, StringComparison.Ordinal);
        Assert.Contains("interface ICustomer_Select", new EntityTypesGenerator().Generate([account, contact], config with { SingleFile = true })[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_IntersectionRejectsMissingEntitiesRatherThanUsingPartialContract()
    {
        var config = DefaultConfig with
        {
            IntersectMapping = new Dictionary<string, IReadOnlyList<string>> { ["ICustomer"] = ["account", "missing"] },
        };
        Assert.Throws<ArgumentException>(() => new EntityTypesGenerator().Generate([TestEntityModels.Account()], config));
    }

    private static IReadOnlyList<GeneratedFile> Generate(XrmQueryGenerationConfig config) =>
        new EntityTypesGenerator().Generate([TestEntityModels.Account(), TestEntityModels.Contact()], config);

    private static string Fixture(string filename) =>
        Normalize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", filename)));

    private static string Normalize(string content) => content.ReplaceLineEndings("\n");
}
