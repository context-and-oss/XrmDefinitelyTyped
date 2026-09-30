using XrmDefinitelyTyped.Core.Domain;
using XrmDefinitelyTyped.Core.Generation.Generators;
using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation;

namespace XrmDefinitelyTyped.Core.Generation;

public sealed class CodeGenerator : ICodeGenerator
{
    private readonly FormGenerator _formGenerator = new();

    public IEnumerable<GeneratedFile> GenerateCode(
        IEnumerable<FormModel> forms,
        IEnumerable<object> customApis,
        XdtGenerationConfig config) =>
        GenerateCode(forms, [], customApis, config);

    public IEnumerable<GeneratedFile> GenerateCode(
        IEnumerable<FormModel> forms,
        IEnumerable<EntityModel> entities,
        IEnumerable<object> customApis,
        XdtGenerationConfig config)
    {
        ArgumentNullException.ThrowIfNull(forms);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(config);

        var files = new List<GeneratedFile>
        {
            new FormSupportGenerator().Generate(),
        };
        files.AddRange(_formGenerator.Generate(forms.ToList(), entities.ToList(), config));

        // Custom API generation is not yet implemented. When added, guard with
        // config.GenerateCustomApis and consume the customApis argument here.

        return files;
    }
}
