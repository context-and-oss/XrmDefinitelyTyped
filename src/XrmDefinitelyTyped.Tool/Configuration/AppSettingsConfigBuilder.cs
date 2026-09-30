using Microsoft.Extensions.Configuration;

namespace XrmDefinitelyTyped.Tool.Configuration;

public static class AppSettingsConfigBuilder
{
    public static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    public static PartialConfig BuildFromConfiguration(IConfiguration configuration)
    {
        var configSection = configuration.GetSection("XrmDefinitelyTyped");

        return new PartialConfig(
            configSection.GetValue<string>("OutputDirectory"),
            configSection.GetSection("Solutions").Get<List<string>>(),
            configSection.GetSection("Entities").Get<List<string>>(),
            configSection.GetSection("LabelMappings").Get<Dictionary<string, string>>(),
            configSection.GetSection("IntersectMapping").Get<Dictionary<string, IReadOnlyList<string>>>(),
            configSection.GetValue<bool?>("SkipInactiveForms"),
            configSection.GetValue<bool?>("SingleFile"),
            configSection.GetValue<bool?>("GenerateCustomApis"),
            configSection.GetValue<bool?>("GenerateMappings"),
            configSection.GetSection("Generate").Get<List<string>>(),
            configSection.GetValue<string>("WebNamespace"));
    }
}