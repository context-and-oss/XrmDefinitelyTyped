using DataverseConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using XrmDefinitelyTyped.Core.Generation;
using XrmDefinitelyTyped.Core.Metadata;
using XrmDefinitelyTyped.Tool;
using XrmDefinitelyTyped.Tool.Configuration;
using XrmQueryTyped.Core.Generation;
using XrmTyped.Shared.Domain;
using XrmTyped.Shared.Generation;
using XrmTyped.Shared.Generation.Generators;
using XrmTyped.Shared.Metadata;
using XrmTyped.Shared.Output;

try
{
    var configuration = AppSettingsConfigBuilder.BuildConfiguration();
    var appSettingsConfig = AppSettingsConfigBuilder.BuildFromConfiguration(configuration);

    // Parse command line args and merge
    var paramsConfig = CommandLineParser.Parse(args);

    var config = new XdtConfig(
        paramsConfig.OutputDirectory ?? appSettingsConfig.OutputDirectory ?? string.Empty,
        new XrmFetchConfig(
            paramsConfig.Solutions ?? appSettingsConfig.Solutions ?? [],
            paramsConfig.Entities ?? appSettingsConfig.Entities ?? [],
            paramsConfig.LabelMappings ?? appSettingsConfig.LabelMappings ?? new Dictionary<string, string>(),
            paramsConfig.SkipInactiveForms ?? appSettingsConfig.SkipInactiveForms ?? false),
        new XdtGenerationConfig(
            paramsConfig.IntersectMapping ?? appSettingsConfig.IntersectMapping ?? new Dictionary<string, IReadOnlyList<string>>(),
            SingleFile: paramsConfig.SingleFile ?? appSettingsConfig.SingleFile ?? false,
            GenerateCustomApis: paramsConfig.GenerateCustomApis ?? appSettingsConfig.GenerateCustomApis ?? false,
            GenerateMappings: paramsConfig.GenerateMappings ?? appSettingsConfig.GenerateMappings ?? false),
        GeneratorKinds.Parse(paramsConfig.Generate ?? appSettingsConfig.Generate ?? []),
        new XrmQueryGenerationConfig(
            paramsConfig.WebNamespace ?? appSettingsConfig.WebNamespace ?? "XDT",
            SingleFile: paramsConfig.SingleFile ?? appSettingsConfig.SingleFile ?? false));

    ConfigValidator.Validate(config);

    // Authentication is handled by the DataverseConnection package, which reads its settings from the
    // IConfiguration registered below: DATAVERSE_URL for the environment, and DATAVERSE_CREDENTIAL_TYPE
    // to pick the credential (browser, devicecode, azcli, ...). Browser and device-code credentials
    // use DataverseConnection's persistent token cache when their option objects remain unset.
    var services = new ServiceCollection();
    services.AddSingleton(configuration);
    services.AddDataverse();
    using var provider = services.BuildServiceProvider();
    var serviceClient = provider.GetRequiredService<ServiceClient>();

    var formSourceFactory = new DataverseMetadataSourceFactory(serviceClient);
    var entitySourceFactory = new DataverseEntityMetadataSourceFactory(serviceClient);
    var files = new List<GeneratedFile>();

    // Entity metadata is shared by both generators. Forms need it for precise option-set,
    // lookup, date and numeric attribute/control types; web generation uses the same model.
    Console.WriteLine($"Fetching entity metadata from {configuration["DATAVERSE_URL"]} ...");
    var entities = await entitySourceFactory
        .CreateEntityMetadataFetcher(MetadataSourceType.Dataverse, config.Fetch)
        .FetchEntityMetadataAsync();
    Console.WriteLine($"Fetched {entities.Count} entity/entities.");

    if (config.Generators.Contains(GeneratorKind.Forms))
    {
        Console.WriteLine($"Fetching form metadata from {configuration["DATAVERSE_URL"]} ...");
        var forms = await formSourceFactory
            .CreateFetcher(MetadataSourceType.Dataverse, config.Fetch)
            .FetchMetadataAsync();
        Console.WriteLine($"Fetched {forms.Count} form(s).");

        files.AddRange(new CodeGenerator().GenerateCode(forms, entities, [], config.Generation));
    }

    if (config.Generators.Contains(GeneratorKind.Web))
        files.AddRange(new EntityTypesGenerator().Generate(entities, config.XrmQuery));

    files.AddRange(new OptionSetGenerator().Generate(entities.SelectMany(entity => entity.OptionSets).ToList()));

    if (config.Generation.SingleFile)
    {
        var runtimeFiles = files.Where(file => file.Filename.EndsWith(".ts", StringComparison.Ordinal) && !file.Filename.EndsWith(".d.ts", StringComparison.Ordinal)).ToList();
        var declarations = files.Where(file => file.Filename.EndsWith(".d.ts", StringComparison.Ordinal)).ToList();
        files =
        [
            .. runtimeFiles,
            new GeneratedFile("context.d.ts", string.Join(Environment.NewLine, declarations.Select(file => file.Content))),
        ];
    }

    // A single write, since the output writer removes only files from its previous manifest.
    var outputFiles = files.DistinctBy(file => file.Filename, StringComparer.Ordinal).ToList();
    Console.WriteLine($"Generated {outputFiles.Count} file(s).");

    new FileSystemOutputWriter().WriteFiles(outputFiles, config.OutputDirectory);
    Console.WriteLine($"Wrote output to {Path.GetFullPath(config.OutputDirectory)}");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
