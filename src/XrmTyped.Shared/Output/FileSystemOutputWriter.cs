using System.Text.Json;
using XrmTyped.Shared.Generation;

namespace XrmTyped.Shared.Output;

public class FileSystemOutputWriter : IOutputWriter
{
    private const string ManifestFilename = ".xdt-manifest.json";

    private static readonly string[] LegacyManagedDirectories =
    [
        "Form",
        "Web",
    ];

    private static readonly string[] LegacyManagedFiles =
    [
        "OptionSets.ts",
        "xrm.d.ts",
        "dg.xrmquery.web.d.ts",
        Path.Combine("Web", "WebEntities.d.ts"),
        Path.Combine("_internal", "Enum", "LCID.d.ts"),
        Path.Combine("_internal", "sdk.d.ts"),
        Path.Combine("_internal", "web-entities.d.ts"),
        Path.Combine("_internal", "WebResources.d.ts"),
    ];

    public void WriteFiles(IEnumerable<GeneratedFile> files, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(files);

        var generatedFiles = files.ToList();
        Directory.CreateDirectory(outputDirectory);
        RemovePreviouslyGeneratedFiles(outputDirectory);

        foreach (var file in generatedFiles)
        {
            var filePath = Path.Combine(outputDirectory, file.Filename);
            var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Unable to determine directory path for file creation.");
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, file.Content);
        }

        var manifestPath = Path.Combine(outputDirectory, ManifestFilename);
        var manifest = generatedFiles.Select(file => NormalizePath(file.Filename)).Order(StringComparer.Ordinal).ToArray();
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void RemovePreviouslyGeneratedFiles(string outputDirectory)
    {
        var manifestPath = Path.Combine(outputDirectory, ManifestFilename);
        var hadManifest = File.Exists(manifestPath);
        if (hadManifest)
        {
            var paths = JsonSerializer.Deserialize<string[]>(File.ReadAllText(manifestPath)) ?? [];
            foreach (var relativePath in paths)
            {
                var fullPath = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));
                if (!fullPath.StartsWith(Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    continue;

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            File.Delete(manifestPath);
        }

        // Migration from the legacy generator: these directories were generator-owned.
        if (!hadManifest)
        {
            foreach (var relativeDirectory in LegacyManagedDirectories)
            {
                var directory = Path.Combine(outputDirectory, relativeDirectory);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        var legacyEnumDirectory = Path.Combine(outputDirectory, "_internal", "Enum");
        if (!hadManifest && Directory.Exists(legacyEnumDirectory))
        {
            foreach (var file in Directory.GetFiles(legacyEnumDirectory, "*.d.ts"))
                File.Delete(file);
        }

        foreach (var relativeFile in LegacyManagedFiles)
        {
            var file = Path.Combine(outputDirectory, relativeFile);
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
