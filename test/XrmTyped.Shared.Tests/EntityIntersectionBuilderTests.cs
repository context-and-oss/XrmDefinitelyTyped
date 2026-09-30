using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation;

namespace XrmTyped.Shared.Tests;

public sealed class EntityIntersectionBuilderTests
{
    [Fact]
    public void Build_OnlyIncludesCompatibleSharedAttributesAndCommonPermissions()
    {
        var name = new AttributeModel("Name", "name", "string", SpecialAttributeType.Default, [], true, true, true);
        var incompatible = name with { LogicalName = "conflicting" };
        var account = new EntityModel(1, "Account", "account", "accounts", "accountid", [name, incompatible], [], []);
        var contact = account with
        {
            SchemaName = "Contact", LogicalName = "contact", EntitySetName = "contacts", PrimaryIdAttribute = "contactid",
            Attributes = [name with { Updateable = false }, incompatible with { TypeScriptType = "number" }],
        };
        var mappings = new Dictionary<string, IReadOnlyList<string>> { ["ICustomer"] = ["ACCOUNT", "contact"] };

        var intersection = Assert.Single(EntityIntersectionBuilder.Build([account, contact], mappings));
        Assert.Equal("ICustomer", intersection.SchemaName);
        Assert.Null(intersection.EntitySetName);
        Assert.Empty(intersection.PrimaryIdAttribute);
        var attribute = Assert.Single(intersection.Attributes);
        Assert.Equal("name", attribute.LogicalName);
        Assert.True(attribute.Readable);
        Assert.True(attribute.Createable);
        Assert.False(attribute.Updateable);
    }

    [Theory]
    [InlineData("", "account")]
    [InlineData("../BadName", "account")]
    [InlineData("ICustomer", "missing")]
    [InlineData("ICustomer", "284FF02B-BDD1-4BB0-9BCF-6CFDBDA130D4")]
    public void Build_RejectsInvalidMappings(string name, string member)
    {
        var account = new EntityModel(1, "Account", "account", "accounts", "accountid", [], [], []);
        var mappings = new Dictionary<string, IReadOnlyList<string>> { [name] = [member] };
        Assert.Throws<ArgumentException>(() => EntityIntersectionBuilder.Build([account], mappings));
    }
}
