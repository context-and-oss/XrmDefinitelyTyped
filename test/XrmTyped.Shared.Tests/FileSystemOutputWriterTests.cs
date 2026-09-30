using XrmTyped.Shared.Generation;
using XrmTyped.Shared.Output;

namespace XrmTyped.Shared.Tests;

public sealed class FileSystemOutputWriterTests
{
    [Fact]
    public void WriteFiles_RemovesLegacySupportFilesAndGeneratedDirectories()
    {
        var output = Path.Combine(Path.GetTempPath(), $"xdt-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(output, "Web"));
            Directory.CreateDirectory(Path.Combine(output, "_internal", "Enum"));
            File.WriteAllText(Path.Combine(output, "xrm.d.ts"), "legacy xrm");
            File.WriteAllText(Path.Combine(output, "dg.xrmquery.web.d.ts"), "legacy xrmquery");
            File.WriteAllText(Path.Combine(output, "_internal", "Enum", "LCID.d.ts"), "lcid");
            File.WriteAllText(Path.Combine(output, "_internal", "Enum", "stale.d.ts"), "stale enum");
            File.WriteAllText(Path.Combine(output, "Web", "stale.d.ts"), "stale");

            new FileSystemOutputWriter().WriteFiles(
                [new GeneratedFile(Path.Combine("Web", "account.d.ts"), "generated")], output);

            Assert.False(File.Exists(Path.Combine(output, "xrm.d.ts")));
            Assert.False(File.Exists(Path.Combine(output, "dg.xrmquery.web.d.ts")));
            Assert.False(File.Exists(Path.Combine(output, "Web", "stale.d.ts")));
            Assert.False(File.Exists(Path.Combine(output, "_internal", "Enum", "stale.d.ts")));
            Assert.False(File.Exists(Path.Combine(output, "_internal", "Enum", "LCID.d.ts")));
            Assert.Equal("generated", File.ReadAllText(Path.Combine(output, "Web", "account.d.ts")));
            Assert.True(File.Exists(Path.Combine(output, ".xdt-manifest.json")));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }
}
