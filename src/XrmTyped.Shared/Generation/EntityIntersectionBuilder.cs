using XrmTyped.Shared.Domain;
using System.Text.RegularExpressions;

namespace XrmTyped.Shared.Generation;

/// <summary>Builds shared contracts, not new Dataverse tables or entity sets.</summary>
public static class EntityIntersectionBuilder
{
    public static IReadOnlyList<EntityModel> Build(
        IReadOnlyList<EntityModel> entities,
        IReadOnlyDictionary<string, IReadOnlyList<string>> mappings)
    {
        var byName = entities.ToDictionary(entity => entity.LogicalName, StringComparer.OrdinalIgnoreCase);
        var intersections = new List<EntityModel>();
        foreach (var mapping in mappings.OrderBy(mapping => mapping.Key, StringComparer.Ordinal))
        {
            var name = mapping.Key;
            if (!Regex.IsMatch(name, @"^[a-zA-Z_$][a-zA-Z0-9_$]*$"))
                throw new ArgumentException($"Invalid intersection interface name '{mapping.Key}'.");
            if (mapping.Value.Count == 0)
                throw new ArgumentException($"Intersection '{name}' must specify entity logical names.");
            var selected = mapping.Value.Distinct(StringComparer.OrdinalIgnoreCase).Select(value =>
                byName.TryGetValue(value, out var entity) ? entity : throw new ArgumentException(
                    $"Entity '{value}' for intersection '{name}' was not found in the fetched metadata.")).ToArray();
            if (entities.Any(entity => entity.SchemaName == name || entity.LogicalName == name))
                throw new ArgumentException($"Intersection '{name}' conflicts with an existing entity name.");

            var attributes = selected[0].Attributes.Select(attribute =>
            {
                var matches = selected.Select(entity => entity.Attributes.FirstOrDefault(candidate =>
                    string.Equals(candidate.LogicalName, attribute.LogicalName, StringComparison.OrdinalIgnoreCase))).ToArray();
                if (matches.Any(match => match is null || match.SpecialType != attribute.SpecialType || match.TypeScriptType != attribute.TypeScriptType))
                    return null;
                return attribute with
                {
                    Readable = matches.All(match => match!.Readable),
                    Createable = matches.All(match => match!.Createable),
                    Updateable = matches.All(match => match!.Updateable),
                    Targets = matches.SelectMany(match => (IEnumerable<TargetEntitySet>)match!.Targets).Distinct().ToArray(),
                };
            }).OfType<AttributeModel>().ToArray();
            var relationships = selected[0].Relationships.Where(relationship =>
                selected.Skip(1).All(entity => entity.Relationships.Contains(relationship))).ToArray();
            var primaryIds = selected.Select(entity => entity.PrimaryIdAttribute).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            intersections.Add(new EntityModel(
                0, name, name, null, primaryIds.Length == 1 ? primaryIds[0] : string.Empty,
                attributes, relationships, []));
        }
        return intersections;
    }
}
