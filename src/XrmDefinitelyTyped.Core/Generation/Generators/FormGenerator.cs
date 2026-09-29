using Scriban;
using System.Reflection;
using XrmDefinitelyTyped.Core.Domain;
using XrmDefinitelyTyped.Core.Generation.Utilities;
using XrmDefinitelyTyped.Core.Generation.ViewModels;
using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation;
using XrmTyped.Shared.Generation.Utilities;

namespace XrmDefinitelyTyped.Core.Generation.Generators;

public sealed class FormGenerator
{
    private const string OutputDirectory = "Form";
    private const string QuickViewClassId = "5C5600E0-1D6E-4205-A272-BE80DA87FD42";

    private static readonly Template Template = TemplateRenderer.Load(
        Assembly.GetExecutingAssembly(),
        "XrmDefinitelyTyped.Core.Templates.form.sbn");

    public IReadOnlyList<GeneratedFile> Generate(
        IReadOnlyList<FormModel> forms,
        IReadOnlyList<EntityModel>? entities = null,
        XdtGenerationConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(forms);

        config ??= new XdtGenerationConfig(new Dictionary<string, IReadOnlyList<string>>());
        var supportedForms = forms
            .Where(IsSupportedForm)
            .ToList();
        supportedForms.AddRange(BuildIntersections(supportedForms, config.IntersectMapping));
        var entitiesByLogicalName = (entities ?? [])
            .ToDictionary(entity => entity.LogicalName, StringComparer.OrdinalIgnoreCase);
        var formsById = supportedForms.ToDictionary(form => form.Id);

        return RenameDuplicateForms(supportedForms)
            .Select(namedForm => GenerateFormFile(
                namedForm.Form,
                namedForm.Name,
                entitiesByLogicalName.GetValueOrDefault(namedForm.Form.MetadataEntityLogicalName ?? namedForm.Form.EntityLogicalName),
                formsById,
                config.GenerateMappings))
            .ToList();
    }

    private static bool IsSupportedForm(FormModel form) =>
        form.FormType is not FormType.Card
            and not FormType.InteractionCentricDashboard
            and not FormType.TaskFlowForm;

    private static IEnumerable<FormModel> BuildIntersections(
        IReadOnlyList<FormModel> forms,
        IReadOnlyDictionary<string, IReadOnlyList<string>> mappings)
    {
        var byId = forms.ToDictionary(form => form.Id);
        foreach (var mapping in mappings.OrderBy(mapping => mapping.Key, StringComparer.Ordinal))
        {
            var selected = mapping.Value
                .Select(value => Guid.TryParse(value, out var id) && byId.TryGetValue(id, out var form) ? form : null)
                .OfType<FormModel>()
                .ToList();
            if (selected.Count == 0)
            {
                Console.Error.WriteLine($"Warning: no forms were found for intersection '{mapping.Key}'.");
                continue;
            }

            var commonControls = IntersectControls(selected);
            var commonTabs = IntersectTabs(selected);
            var metadataEntity = selected.Select(form => form.EntityLogicalName).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).ToArray();
            yield return new FormModel(
                Guid.Empty,
                "_special",
                mapping.Key,
                FormType.Other,
                commonTabs,
                commonControls,
                IsIntersection: true,
                MetadataEntityLogicalName: metadataEntity.Length == 1 ? metadataEntity[0] : null);
        }
    }

    private static IReadOnlyList<ControlModel> IntersectControls(IReadOnlyList<FormModel> forms)
    {
        static IEnumerable<ControlModel> AllControls(FormModel form) => form.Tabs
            .SelectMany(tab => tab.Sections)
            .SelectMany(section => section.Controls)
            .Concat(form.AdditionalControls ?? []);
        static string Key(ControlModel control) => $"{control.Id}\u001f{control.DataFieldName}\u001f{control.ClassId}";

        var first = AllControls(forms[0]).ToDictionary(Key, StringComparer.Ordinal);
        var common = first.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var form in forms.Skip(1))
            common.IntersectWith(AllControls(form).Select(Key));
        return [.. common.Order(StringComparer.Ordinal).Select(key => first[key])];
    }

    private static IReadOnlyList<TabModel> IntersectTabs(IReadOnlyList<FormModel> forms)
    {
        var commonTabNames = forms[0].Tabs.Select(tab => tab.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var form in forms.Skip(1))
            commonTabNames.IntersectWith(form.Tabs.Select(tab => tab.Name));

        return [.. commonTabNames.Order(StringComparer.Ordinal).Select(tabName =>
        {
            var matchingTabs = forms.Select(form => form.Tabs.First(tab => tab.Name == tabName)).ToList();
            var commonSections = matchingTabs[0].Sections.Select(section => section.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var tab in matchingTabs.Skip(1))
                commonSections.IntersectWith(tab.Sections.Select(section => section.Name));
            return new TabModel(
                tabName,
                matchingTabs[0].Label,
                [.. commonSections.Order(StringComparer.Ordinal).Select(name => new SectionModel(name, name, []))]);
        })];
    }

    private static string ResolveValueType(AttributeModel? attribute) => attribute switch
    {
        null => "any",
        { SpecialType: SpecialAttributeType.MultiSelectOptionSet } => $"{attribute.TypeScriptType}[]",
        { SpecialType: SpecialAttributeType.EntityReference } => $"XDTForm.LookupValue<{BuildLookupTargetType(attribute)}>[]",
        _ => attribute.TypeScriptType,
    };

    private static IEnumerable<(FormModel Form, string Name)> RenameDuplicateForms(IReadOnlyList<FormModel> forms) =>
        forms
            .GroupBy(form => (form.EntityLogicalName, form.FormType, Name: TypeScriptIdentifier.RemoveInvalidCharacters(form.Name)))
            .SelectMany(group => group
                .OrderBy(form => form.Id)
                .Select((form, index) => (form, index == 0 ? group.Key.Name : $"{group.Key.Name}{index}")));

    private static GeneratedFile GenerateFormFile(
        FormModel form,
        string generatedName,
        EntityModel? entity,
        IReadOnlyDictionary<Guid, FormModel> formsById,
        bool generateMappings)
    {
        var viewModel = BuildFormViewModel(form, generatedName, entity, formsById, generateMappings);
        var content = TemplateRenderer.Render(Template, viewModel);
        var filename = form.IsIntersection
            ? Path.Combine(OutputDirectory, form.EntityLogicalName, $"{viewModel.FormName}.d.ts")
            : Path.Combine(OutputDirectory, form.EntityLogicalName, form.FormType.ToString(), $"{viewModel.FormName}.d.ts");
        return new GeneratedFile(filename, content);
    }

    private static FormViewModel BuildFormViewModel(
        FormModel form,
        string generatedName,
        EntityModel? entity,
        IReadOnlyDictionary<Guid, FormModel> formsById,
        bool generateMappings)
    {
        var tabs = form.Tabs
            .Where(tab => !string.IsNullOrWhiteSpace(tab.Name))
            .Select(BuildTabViewModel)
            .OrderBy(tab => tab.Identifier, StringComparer.Ordinal)
            .ToList();
        var metadataByLogicalName = (entity?.Attributes ?? [])
            .ToDictionary(attribute => attribute.LogicalName, StringComparer.OrdinalIgnoreCase);

        var allControls = form.Tabs
            .SelectMany(t => t.Sections)
            .SelectMany(s => s.Controls)
            .Concat(form.AdditionalControls ?? [])
            .ToList();

        var attributes = allControls
            .Where(control => control.DataFieldName is not null && !IsQuickViewControl(control))
            .GroupBy(control => control.DataFieldName!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var control = group.First();
                metadataByLogicalName.TryGetValue(group.Key, out var metadata);
                var attributeType = ResolveAttributeType(control, metadata);
                return attributeType is null
                    ? null
                    : new AttributeViewModel(
                        group.Key,
                        Nullable(attributeType, group.Any(item => item.CanBeNull)),
                        Nullable(ResolveValueType(metadata), group.Any(item => item.CanBeNull)));
            })
            .OfType<AttributeViewModel>()
            .OrderBy(attribute => attribute.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var controls = RenameDuplicateControls(allControls.Where(control => !IsQuickViewControl(control)))
            .Select(namedControl =>
            {
                var metadata = namedControl.Control.DataFieldName is not null
                    ? metadataByLogicalName.GetValueOrDefault(namedControl.Control.DataFieldName)
                    : null;
                var type = ResolveControlType(namedControl.Control, metadata);
                return new ControlViewModel(namedControl.Name, Nullable(type, namedControl.Control.CanBeNull));
            })
            .OrderBy(control => control.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var quickViewForms = allControls
            .Where(IsQuickViewControl)
            .Select(control => new QuickViewFormViewModel(control.Id, ResolveQuickViewFormType(control, formsById)))
            .DistinctBy(quickView => quickView.ControlId, StringComparer.Ordinal)
            .OrderBy(quickView => quickView.ControlId, StringComparer.Ordinal)
            .ToList();

        return new FormViewModel(
            form.EntityLogicalName,
            form.IsIntersection ? string.Empty : form.FormType.ToString(),
            generatedName,
            form.FormType == FormType.Quick,
            generateMappings,
            tabs,
            attributes,
            controls,
            quickViewForms);
    }

    private static IEnumerable<(ControlModel Control, string Name)> RenameDuplicateControls(IEnumerable<ControlModel> controls) =>
        controls
            .GroupBy(control => control.Id, StringComparer.Ordinal)
            .SelectMany(group => group.Select((control, index) =>
                (control, index == 0
                    ? group.Key
                    : control.IsBusinessProcessFlow ? $"{group.Key}_{index}" : $"{group.Key}{index}")));

    private static string ResolveQuickViewFormType(ControlModel control, IReadOnlyDictionary<Guid, FormModel> formsById)
    {
        if (control.QuickViewFormId is not Guid formId || !formsById.TryGetValue(formId, out var form))
            return "XDTForm.QuickViewFormBase";

        return $"Form.{form.EntityLogicalName}.{form.FormType}.{TypeScriptIdentifier.RemoveInvalidCharacters(form.Name)}";
    }

    private static bool IsQuickViewControl(ControlModel control) =>
        string.Equals(control.ClassId.Trim('{', '}'), QuickViewClassId, StringComparison.OrdinalIgnoreCase)
        || control.QuickViewFormId is not null;

    private static string? ResolveAttributeType(ControlModel control, AttributeModel? attribute)
    {
        if (attribute is null)
            return ClassIdMap.Resolve(control.ClassId).AttributeType;

        return attribute.SpecialType switch
        {
            SpecialAttributeType.OptionSet => $"Xrm.Attributes.OptionSetAttribute<{attribute.TypeScriptType}>",
            SpecialAttributeType.MultiSelectOptionSet => $"Xrm.Attributes.MultiSelectOptionSetAttribute<{attribute.TypeScriptType}>",
            SpecialAttributeType.EntityReference => $"XDTForm.LookupAttribute<{BuildLookupTargetType(attribute)}>",
            _ when attribute.TypeScriptType == "Date" => "Xrm.Attributes.DateAttribute",
            _ when attribute.TypeScriptType == "number" => "Xrm.Attributes.NumberAttribute",
            _ when attribute.TypeScriptType == "boolean" && IsOptionSetControl(control) => "Xrm.Attributes.BooleanAttribute",
            _ => $"Xrm.Attributes.Attribute<{attribute.TypeScriptType}>",
        };
    }

    private static string ResolveControlType(ControlModel control, AttributeModel? attribute)
    {
        var classType = ClassIdMap.Resolve(control.ClassId).ControlType;
        if (classType.StartsWith("XDTForm.SubGridControl<", StringComparison.Ordinal))
            return $"XDTForm.SubGridControl<{BuildTargetType(control.TargetEntityTypes)}>";

        if (attribute is null)
            return classType;

        return attribute.SpecialType switch
        {
            SpecialAttributeType.OptionSet => $"XDTForm.OptionSetControl<{attribute.TypeScriptType}>",
            SpecialAttributeType.MultiSelectOptionSet => $"XDTForm.MultiSelectOptionSetControl<{attribute.TypeScriptType}>",
            SpecialAttributeType.EntityReference => $"XDTForm.LookupControl<{BuildLookupTargetType(attribute)}>",
            _ when attribute.TypeScriptType == "Date" => "Xrm.Controls.DateControl",
            _ when attribute.TypeScriptType == "number" => "Xrm.Controls.NumberControl",
            _ when attribute.TypeScriptType == "string" => "Xrm.Controls.StringControl",
            _ when attribute.TypeScriptType == "boolean" && IsOptionSetControl(control) => "XDTForm.BooleanControl",
            _ => "Xrm.Controls.StandardControl",
        };
    }

    private static string Nullable(string type, bool canBeNull) => canBeNull ? $"{type} | null" : type;

    private static bool IsOptionSetControl(ControlModel control) =>
        ClassIdMap.Resolve(control.ClassId).ControlType.StartsWith("XDTForm.OptionSetControl<", StringComparison.Ordinal);

    private static string BuildLookupTargetType(AttributeModel attribute) =>
        BuildTargetType(attribute.Targets.Select(target => target.LogicalName));

    private static string BuildTargetType(IEnumerable<string>? logicalNames)
    {
        var targets = (logicalNames ?? [])
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Select(target => $"\"{target}\"")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();

        return targets.Length == 0 ? "string" : string.Join(" | ", targets);
    }

    private static TabViewModel BuildTabViewModel(TabModel tab)
    {
        var sections = tab.Sections
            .Where(section => !string.IsNullOrWhiteSpace(section.Name))
            .Select(section => new SectionViewModel(section.Name))
            .OrderBy(section => section.Name, StringComparer.Ordinal)
            .ToList();
        return new TabViewModel(TypeScriptIdentifier.RemoveInvalidCharacters(tab.Name), tab.Name, sections);
    }
}
