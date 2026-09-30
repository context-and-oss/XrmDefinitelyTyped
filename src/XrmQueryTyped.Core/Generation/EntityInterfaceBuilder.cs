using XrmTyped.Shared.Domain;
using XrmQueryTyped.Core.Generation.ViewModels;

namespace XrmQueryTyped.Core.Generation;

internal static class EntityInterfaceBuilder
{
    private const string OwnerAttributeName = "ownerid";
    private const string CurrencyGuidName = "transactioncurrencyid_guid";
    private const string CurrencyFormattedName = "transactioncurrencyid_formatted";
    private const string GuidType = "XQW.Guid";
    private const string EmptyObjectType = "object";

    private sealed record ResultVariable(string Name, string Type, string FormattedName);

    public static IReadOnlyList<WebInterfaceViewModel> Build(EntityModel entity)
    {
        var name = entity.SchemaName;
        var attributeNames = entity.Attributes.Select(attribute => attribute.LogicalName).ToHashSet(StringComparer.Ordinal);
        var navigableRelationships = entity.Relationships
            .Where(relationship => relationship.IsRelatedEntityIncluded)
            .ToArray();

        // A navigation property whose name collides with an attribute cannot also be a result member,
        // since the attribute already occupies that name in the result payload.
        var resultRelationships = navigableRelationships
            .Where(relationship => !attributeNames.Contains(relationship.NavigationPropertyName))
            .ToArray();

        return
        [
            new(InterfaceNames.Fixed(name), string.Empty, string.IsNullOrEmpty(entity.PrimaryIdAttribute) ? [] : [new(entity.PrimaryIdAttribute, string.Empty, "string")]),
            new(InterfaceNames.Result(name), $" extends {InterfaceNames.Fixed(name)}", BuildResultMembers(entity, resultRelationships)),
            new(InterfaceNames.FormattedResult(name), string.Empty, BuildFormattedResultMembers(entity)),
            new(InterfaceNames.Select(name), string.Empty, BuildSelectMembers(entity)),
            new(InterfaceNames.Filter(name), string.Empty, BuildFilterMembers(entity)),
            new(InterfaceNames.Expand(name), string.Empty, BuildExpandMembers(entity, navigableRelationships)),
            new(InterfaceNames.Create(name), string.Empty, BuildWriteMembers(entity, attributeNames, forCreate: true)),
            new(InterfaceNames.Update(name), string.Empty, BuildWriteMembers(entity, attributeNames, forCreate: false)),
            new(InterfaceNames.RelatedOne(name), string.Empty, BuildRelatedMembers(navigableRelationships, referencing: true)),
            new(InterfaceNames.RelatedMany(name), string.Empty, BuildRelatedMembers(navigableRelationships, referencing: false)),
        ];
    }

    private static IReadOnlyList<WebMemberViewModel> BuildResultMembers(
        EntityModel entity,
        IReadOnlyList<RelationshipModel> navigableRelationships)
    {
        var attributeMembers = entity.Attributes
            .Where(attribute => attribute.Readable)
            .SelectMany(GetResultVariables)
            .Where(variable => variable.Name != entity.PrimaryIdAttribute)
            .Select(variable => new WebMemberViewModel(variable.Name, string.Empty, Nullable(variable.Type)));

        var relationshipMembers = navigableRelationships
            .Select(relationship => new WebMemberViewModel(
                relationship.NavigationPropertyName,
                "?",
                Nullable(ResultType(relationship))));

        return FinalizeDistinct([.. attributeMembers, .. relationshipMembers]);
    }

    private static IReadOnlyList<WebMemberViewModel> BuildFormattedResultMembers(EntityModel entity) =>
        FinalizeDistinct([.. entity.Attributes
            .Where(attribute => attribute.Readable && HasFormattedValue(attribute))
            .SelectMany(GetResultVariables)
            .Select(variable => new WebMemberViewModel(variable.FormattedName, "?", "string"))]);

    private static IReadOnlyList<WebMemberViewModel> BuildSelectMembers(EntityModel entity) =>
        Finalize([.. entity.Attributes.Select(attribute =>
        {
            var variables = GetResultVariables(attribute);
            var resultType = InlineObject([.. variables.Select(variable => $"{variable.Name}: {Nullable(variable.Type)}")]);
            var formattedType = HasFormattedValue(attribute)
                ? InlineObject([.. variables.Select(variable => $"{variable.FormattedName}?: string")])
                : EmptyObjectType;

            return new WebMemberViewModel(
                MemberName(attribute),
                string.Empty,
                $"WebAttribute<{InterfaceNames.Select(entity.SchemaName)}, {resultType}, {formattedType}>");
        })]);

    private static IReadOnlyList<WebMemberViewModel> BuildFilterMembers(EntityModel entity) =>
        Finalize([.. entity.Attributes.Select(attribute => new WebMemberViewModel(
            MemberName(attribute),
            string.Empty,
            attribute.SpecialType switch
            {
                SpecialAttributeType.EntityReference or SpecialAttributeType.Guid => GuidType,
                _ => attribute.TypeScriptType,
            }))]);

    private static IReadOnlyList<WebMemberViewModel> BuildExpandMembers(
        EntityModel entity,
        IReadOnlyList<RelationshipModel> navigableRelationships) =>
        Finalize(MergeOwner([.. navigableRelationships.Select(relationship =>
        {
            var navigationProperty = relationship.NavigationPropertyName;
            var resultType = InlineObject([$"{navigationProperty}: {ResultType(relationship)}"]);

            return new WebMemberViewModel(
                navigationProperty,
                string.Empty,
                $"WebExpand<{InterfaceNames.Expand(entity.SchemaName)}, {InterfaceNames.Select(relationship.RelatedSchemaName)}, " +
                $"{InterfaceNames.Filter(relationship.RelatedSchemaName)}, {resultType}>");
        })]));

    private static IReadOnlyList<WebMemberViewModel> BuildRelatedMembers(
        IReadOnlyList<RelationshipModel> navigableRelationships,
        bool referencing) =>
        Finalize(MergeOwner([.. navigableRelationships
            .Where(relationship => relationship.IsReferencing == referencing)
            .Select(relationship => new WebMemberViewModel(
                relationship.NavigationPropertyName,
                string.Empty,
                InterfaceNames.RetrieveMapping(relationship.RelatedSchemaName)))]));

    private static IReadOnlyList<WebMemberViewModel> BuildWriteMembers(
        EntityModel entity,
        IReadOnlySet<string> attributeNames,
        bool forCreate)
    {
        var attributeMembers = entity.Attributes
            .Where(attribute => IsWritable(attribute, forCreate) && attribute.SpecialType != SpecialAttributeType.EntityReference)
            .Select(attribute => new WebMemberViewModel(attribute.LogicalName, "?", Nullable(attribute.TypeScriptType)));

        var writableAttributes = entity.Attributes
            .Where(attribute => IsWritable(attribute, forCreate))
            .Select(attribute => attribute.LogicalName)
            .ToHashSet(StringComparer.Ordinal);

        var bindMembers = entity.Relationships
            .Where(relationship => relationship.IsReferencing
                && attributeNames.Contains(relationship.AttributeName)
                && writableAttributes.Contains(relationship.AttributeName))
            .Select(relationship => new WebMemberViewModel(
                $"{relationship.NavigationPropertyName}_bind${relationship.RelatedEntitySetName}",
                "?",
                Nullable("string")));

        return Finalize([.. attributeMembers, .. bindMembers]);
    }

    private static bool IsWritable(AttributeModel attribute, bool forCreate) =>
        forCreate ? attribute.Createable : attribute.Updateable;

    private static ResultVariable[] GetResultVariables(AttributeModel attribute) =>
        attribute.SpecialType switch
        {
            SpecialAttributeType.EntityReference =>
                [new(GuidName(attribute), "string", FormattedName(attribute))],
            SpecialAttributeType.Money =>
                [
                    new(attribute.LogicalName, attribute.TypeScriptType, FormattedName(attribute)),
                    new(CurrencyGuidName, "string", CurrencyFormattedName),
                ],
            _ => [new(attribute.LogicalName, attribute.TypeScriptType, FormattedName(attribute))],
        };

    private static string MemberName(AttributeModel attribute) =>
        attribute.SpecialType == SpecialAttributeType.EntityReference ? GuidName(attribute) : attribute.LogicalName;

    private static bool HasFormattedValue(AttributeModel attribute) =>
        attribute.SpecialType is SpecialAttributeType.EntityReference
            or SpecialAttributeType.Money
            or SpecialAttributeType.OptionSet
            or SpecialAttributeType.MultiSelectOptionSet
            or SpecialAttributeType.Decimal
        || attribute.TypeScriptType == "Date";

    private static string GuidName(AttributeModel attribute) => $"{attribute.LogicalName}_guid";

    private static string FormattedName(AttributeModel attribute) => $"{attribute.LogicalName}_formatted";

    private static string ResultType(RelationshipModel relationship) =>
        relationship.IsReferencing
            ? InterfaceNames.Result(relationship.RelatedSchemaName)
            : $"{InterfaceNames.Result(relationship.RelatedSchemaName)}[]";

    private static string Nullable(string type) => $"{type} | null";

    private static string InlineObject(IReadOnlyList<string> members) => $"{{ {string.Join("; ", members)} }}";

    private static WebMemberViewModel[] MergeOwner(IReadOnlyList<WebMemberViewModel> members)
    {
        var ownerMembers = members.Where(member => member.Name == OwnerAttributeName).ToArray();
        if (ownerMembers.Length < 2)
        {
            return [.. members];
        }

        var merged = new WebMemberViewModel(
            OwnerAttributeName,
            ownerMembers[0].Optional,
            string.Join(" & ", ownerMembers.Select(member => member.Type)));

        return [.. members.Where(member => member.Name != OwnerAttributeName), merged];
    }

    private static WebMemberViewModel[] Finalize(IReadOnlyList<WebMemberViewModel> members)
    {
        var uniqueMembers = members
            .GroupBy(member => member.Name, StringComparer.Ordinal)
            .SelectMany(group => group.Select((member, index) =>
                index == 0 ? member : member with { Name = $"{member.Name}{index}" }));

        return [.. uniqueMembers.OrderBy(member => member.Name, StringComparer.Ordinal)];
    }

    private static WebMemberViewModel[] FinalizeDistinct(IReadOnlyList<WebMemberViewModel> members) =>
    [
        .. members
            .DistinctBy(member => member.Name, StringComparer.Ordinal)
            .OrderBy(member => member.Name, StringComparer.Ordinal),
    ];
}
