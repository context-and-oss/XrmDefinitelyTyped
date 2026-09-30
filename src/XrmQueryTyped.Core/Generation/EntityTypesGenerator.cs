using Scriban;
using System.Reflection;
using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation;
using XrmTyped.Shared.Generation.Utilities;
using XrmQueryTyped.Core.Generation.ViewModels;

namespace XrmQueryTyped.Core.Generation;

public sealed class EntityTypesGenerator
{
    private const string OutputDirectory = "Web";
    private const string SingleFileName = "WebEntities.d.ts";

    private static readonly Template Template = TemplateRenderer.Load(
        Assembly.GetExecutingAssembly(),
        "XrmQueryTyped.Core.Templates.webentity.sbn");

    public IReadOnlyList<GeneratedFile> Generate(IReadOnlyList<EntityModel> entities, XrmQueryGenerationConfig config)
    {
        var intersections = EntityIntersectionBuilder.Build(entities, config.IntersectMapping ?? new Dictionary<string, IReadOnlyList<string>>());
        var rendered = entities.Concat(intersections)
            .OrderBy(entity => entity.LogicalName, StringComparer.Ordinal)
            .Select(entity => (entity.LogicalName, Content: TemplateRenderer.Render(Template, BuildViewModel(entity, config))))
            .ToArray();

        if (config.SingleFile)
        {
            var content = string.Join(Environment.NewLine, rendered.Select(file => file.Content));
            return [new GeneratedFile(Path.Combine(OutputDirectory, SingleFileName), content)];
        }

        return [.. rendered.Select(file =>
            new GeneratedFile(Path.Combine(OutputDirectory, $"{file.LogicalName}.d.ts"), file.Content))];
    }

    private static WebEntityFileViewModel BuildViewModel(EntityModel entity, XrmQueryGenerationConfig config)
    {
        var hasNamespace = !string.IsNullOrWhiteSpace(config.Namespace);
        var prefix = hasNamespace ? $"{config.Namespace}." : string.Empty;

        return new WebEntityFileViewModel(
            config.Namespace,
            hasNamespace,
            hasNamespace ? "  " : string.Empty,
            EntityInterfaceBuilder.Build(entity),
            BuildEntitySets(entity, prefix));
    }

    private static WebEntitySetViewModel[] BuildEntitySets(EntityModel entity, string prefix)
    {
        if (entity.EntitySetName is null)
        {
            return [];
        }

        return
        [
            new WebEntitySetViewModel(
                entity.EntitySetName,
                InterfaceNames.RetrieveMapping(entity.SchemaName, prefix),
                InterfaceNames.RelatedMapping(entity.SchemaName, prefix),
                InterfaceNames.CreateUpdateDeleteAssociateMapping(entity.SchemaName, prefix)),
        ];
    }
}
