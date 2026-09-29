using Scriban;
using System.Reflection;
using XrmTyped.Shared.Generation;
using XrmTyped.Shared.Generation.Utilities;

namespace XrmDefinitelyTyped.Core.Generation.Generators;

public sealed class FormSupportGenerator
{
    private static readonly Template Template = TemplateRenderer.Load(
        Assembly.GetExecutingAssembly(),
        "XrmDefinitelyTyped.Core.Templates.form-support.sbn");

    public GeneratedFile Generate() =>
        new(Path.Combine("_internal", "XrmDefinitelyTyped.d.ts"), TemplateRenderer.Render(Template, new { }));
}
