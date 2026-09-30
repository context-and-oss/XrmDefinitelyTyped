using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using XrmDefinitelyTyped.Core.Domain;
using XrmDefinitelyTyped.Core.Generation;
using XrmTyped.Shared.Metadata;

namespace XrmDefinitelyTyped.Core.Metadata;

public sealed partial class DataverseMetadataFetcher(ServiceClient serviceClient, XrmFetchConfig config) : IDataverseMetadataFetcher
{
    private const int MaxParallelism = 8;
    private const string CustomControlClassId = "F9A8A302-114E-466A-B582-6771B2AE0D92";
    private const string TextBoxClassId = "4273EDBD-AC1D-40D3-9FB2-095C621B552D";

    private static readonly string[] AddressCompositeSuffixes =
    [
        "line1", "line2", "line3", "city", "stateorprovince", "postalcode", "country",
    ];

    private readonly EntitySelectionResolver entitySelectionResolver = new(serviceClient, config);

    [GeneratedRegex("^address(\\d)_composite$", RegexOptions.IgnoreCase)]
    private static partial Regex CompositeAddressRegex();

    public async Task<IReadOnlyList<FormModel>> FetchMetadataAsync()
    {
        var entityNames = await entitySelectionResolver.ResolveEntityNamesAsync();
        var formsTask = RetrieveAllPagesAsync(BuildFormQuery(entityNames));
        var bpfTask = FetchBusinessProcessFlowControlsAsync(entityNames);
        await Task.WhenAll(formsTask, bpfTask);

        var bpfControls = await bpfTask;
        return (await formsTask)
            .AsParallel()
            .WithDegreeOfParallelism(MaxParallelism)
            .Select(entity => ParseForm(entity, bpfControls))
            .ToList();
    }

    private QueryExpression BuildFormQuery(IReadOnlyList<string> entityNames)
    {
        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("formid", "objecttypecode", "name", "type", "formxml"),
            Criteria = new FilterExpression(LogicalOperator.And),
        };

        if (config.SkipInactiveForms)
            query.Criteria.AddCondition("formactivationstate", ConditionOperator.Equal, 1);

        AddEntityNameCondition(query, "objecttypecode", entityNames);
        return query;
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<ControlModel>>> FetchBusinessProcessFlowControlsAsync(
        IReadOnlyList<string> entityNames)
    {
        var query = new QueryExpression("workflow")
        {
            ColumnSet = new ColumnSet("clientdata"),
            Criteria = new FilterExpression(LogicalOperator.And),
        };
        query.Criteria.AddCondition("category", ConditionOperator.Equal, 4);
        query.Criteria.AddCondition("clientdata", ConditionOperator.NotNull);

        var selectedNames = entityNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var controls = new Dictionary<string, List<ControlModel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var workflow in await RetrieveAllPagesAsync(query))
        {
            var clientData = workflow.GetAttributeValue<string>("clientdata");
            if (string.IsNullOrWhiteSpace(clientData))
                continue;

            try
            {
                using var document = JsonDocument.Parse(clientData);
                CollectBusinessProcessFlowControls(document.RootElement, null, selectedNames, controls);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Warning: unable to parse BPF workflow {workflow.Id}: {ex.Message}");
            }
        }

        return controls.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<ControlModel>)entry.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void CollectBusinessProcessFlowControls(
        JsonElement element,
        string? currentEntity,
        IReadOnlySet<string> selectedNames,
        IDictionary<string, List<ControlModel>> controls)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                CollectBusinessProcessFlowControls(child, currentEntity, selectedNames, controls);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        var className = GetJsonString(element, "__class") ?? string.Empty;
        if (className.StartsWith("EntityStep", StringComparison.Ordinal))
            currentEntity = GetJsonString(element, "description") ?? currentEntity;

        if (className.StartsWith("ControlStep", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(currentEntity)
            && (selectedNames.Count == 0 || selectedNames.Contains(currentEntity)))
        {
            var attributeName = GetJsonString(element, "dataFieldName");
            if (!string.IsNullOrWhiteSpace(attributeName))
            {
                var target = element.TryGetProperty("entity", out var relatedEntity)
                    ? GetJsonString(relatedEntity, "entityName")
                    : null;
                var control = new ControlModel(
                    $"header_process_{attributeName}",
                    attributeName,
                    GetJsonString(element, "classId") ?? string.Empty,
                    string.IsNullOrWhiteSpace(target) ? [] : [target],
                    CanBeNull: true,
                    IsBusinessProcessFlow: true);

                if (!controls.TryGetValue(currentEntity, out var entityControls))
                {
                    entityControls = [];
                    controls[currentEntity] = entityControls;
                }
                entityControls.Add(control);
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                CollectBusinessProcessFlowControls(property.Value, currentEntity, selectedNames, controls);
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static void AddEntityNameCondition(QueryExpression query, string attributeName, IReadOnlyList<string> entityNames)
    {
        if (entityNames.Count == 0)
            return;

        var condition = new ConditionExpression(attributeName, ConditionOperator.In);
        foreach (var name in entityNames)
            condition.Values.Add(name);
        query.Criteria.Conditions.Add(condition);
    }

    private async Task<IReadOnlyList<Entity>> RetrieveAllPagesAsync(QueryExpression query)
    {
        var results = new List<Entity>();
        query.PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 };

        while (true)
        {
            var response = await serviceClient.RetrieveMultipleAsync(query);
            results.AddRange(response.Entities);

            if (!response.MoreRecords)
                break;

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = response.PagingCookie;
        }

        return results;
    }

    private FormModel ParseForm(
        Entity entity,
        IReadOnlyDictionary<string, IReadOnlyList<ControlModel>> bpfControls)
    {
        var id = entity.Id;
        var entityLogicalName = entity.GetAttributeValue<string>("objecttypecode") ?? string.Empty;
        var name = entity.GetAttributeValue<string>("name") ?? string.Empty;
        var typeOptionSet = entity.GetAttributeValue<OptionSetValue>("type");
        var formType = (FormType)(typeOptionSet?.Value ?? 0);
        var formXml = entity.GetAttributeValue<string>("formxml") ?? "<form/>";

        var formElement = XDocument.Parse(formXml).Root ?? new XElement("form");
        var controlClassIds = ParseCustomControlClassIds(formElement);
        var tabs = ParseTabs(formElement, config.LabelMappings, controlClassIds);
        bpfControls.TryGetValue(entityLogicalName, out var additionalControls);

        return new FormModel(id, entityLogicalName, name, formType, tabs, additionalControls);
    }

    private static IReadOnlyDictionary<string, string> ParseCustomControlClassIds(XElement formElement) =>
        formElement.Descendants("controlDescription")
            .Select(description => new
            {
                ForControl = description.Attribute("forControl")?.Value,
                ClassId = description.Elements("customControl").Select(control => control.Attribute("id")?.Value).FirstOrDefault(value => value is not null),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ForControl) && !string.IsNullOrWhiteSpace(item.ClassId))
            .ToDictionary(item => item.ForControl!, item => item.ClassId!, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<TabModel> ParseTabs(
        XElement formElement,
        IReadOnlyDictionary<string, string> labelMappings,
        IReadOnlyDictionary<string, string> customControlClassIds)
    {
        return formElement
            .Element("tabs")?
            .Elements("tab")
            .Select(tab => new TabModel(
                tab.Attribute("name")?.Value ?? string.Empty,
                GetLabel(tab, labelMappings),
                ParseSections(tab, labelMappings, customControlClassIds)))
            .ToList() ?? [];
    }

    private static IReadOnlyList<SectionModel> ParseSections(
        XElement tabElement,
        IReadOnlyDictionary<string, string> labelMappings,
        IReadOnlyDictionary<string, string> customControlClassIds)
    {
        return tabElement
            .Element("columns")?
            .Elements("column")
            .SelectMany(column => column.Element("sections")?.Elements("section") ?? [])
            .Select(section => new SectionModel(
                section.Attribute("name")?.Value ?? string.Empty,
                GetLabel(section, labelMappings),
                ParseControls(section, customControlClassIds)))
            .ToList() ?? [];
    }

    private static IReadOnlyList<ControlModel> ParseControls(
        XElement sectionElement,
        IReadOnlyDictionary<string, string> customControlClassIds)
    {
        return sectionElement
            .Element("rows")?
            .Elements("row")
            .SelectMany(row => row.Elements("cell"))
            .SelectMany(cell => cell.Elements("control"))
            .SelectMany(control => ExpandCompositeControl(ParseControl(control, customControlClassIds)))
            .ToList() ?? [];
    }

    private static ControlModel ParseControl(
        XElement control,
        IReadOnlyDictionary<string, string> customControlClassIds)
    {
        var id = control.Attribute("id")?.Value ?? string.Empty;
        var classId = NormalizeClassId(control.Attribute("classid")?.Value);
        var uniqueId = control.Attribute("uniqueid")?.Value ?? string.Empty;
        if (string.Equals(classId, CustomControlClassId, StringComparison.OrdinalIgnoreCase)
            && customControlClassIds.TryGetValue(uniqueId, out var customClassId))
        {
            classId = NormalizeClassId(customClassId);
        }

        if (id.StartsWith("WebResource_", StringComparison.Ordinal))
            classId = ClassIdMap.WebResourceClassId;

        var dataFieldName = control.Attribute("datafieldname")?.Value;
        var targets = control.Descendants("TargetEntityType")
            .Select(target => target.Value)
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var (quickViewFormId, quickViewEntityName) = ParseQuickViewReference(control);
        var nullable = string.Equals(dataFieldName, "fullname", StringComparison.OrdinalIgnoreCase)
            || (dataFieldName is not null && CompositeAddressRegex().IsMatch(dataFieldName));

        return new ControlModel(
            id,
            dataFieldName,
            classId,
            targets,
            nullable,
            IsBusinessProcessFlow: false,
            quickViewFormId,
            quickViewEntityName);
    }

    private static IEnumerable<ControlModel> ExpandCompositeControl(ControlModel control)
    {
        yield return control;
        if (control.DataFieldName is null)
            yield break;

        var match = CompositeAddressRegex().Match(control.DataFieldName);
        if (!match.Success)
            yield break;

        var prefix = $"address{match.Groups[1].Value}_";
        foreach (var suffix in AddressCompositeSuffixes)
        {
            var attributeName = prefix + suffix;
            yield return new ControlModel(
                $"{control.Id}_compositionLinkControl_{attributeName}",
                attributeName,
                TextBoxClassId,
                CanBeNull: true);
        }
    }

    private static (Guid? FormId, string? EntityName) ParseQuickViewReference(XElement control)
    {
        var xml = control.Descendants("QuickForms").Select(element => element.Value).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(xml))
            return (null, null);

        try
        {
            var quickForm = XDocument.Parse(xml).Descendants("QuickFormId").FirstOrDefault();
            return (
                Guid.TryParse(quickForm?.Value, out var formId) ? formId : null,
                quickForm?.Attribute("entityname")?.Value);
        }
        catch (System.Xml.XmlException)
        {
            return (null, null);
        }
    }

    private static string NormalizeClassId(string? classId) => (classId ?? string.Empty).Trim('{', '}').ToUpperInvariant();

    private static string GetLabel(XElement element, IReadOnlyDictionary<string, string> labelMappings)
    {
        var labels = element.Element("labels")?.Elements("label").ToList() ?? [];
        var text = labels.FirstOrDefault(label => label.Attribute("languagecode")?.Value == "1033")?.Attribute("description")?.Value
            ?? labels.FirstOrDefault()?.Attribute("description")?.Value
            ?? string.Empty;

        return labelMappings.Aggregate(text, (current, mapping) => current.Replace(mapping.Key, mapping.Value, StringComparison.Ordinal));
    }
}
