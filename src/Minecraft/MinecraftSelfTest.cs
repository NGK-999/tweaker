using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.Minecraft.Services;
using ApexTweaker.UI.Wpf.Views;

namespace ApexTweaker.Minecraft;

internal static class MinecraftSelfTest
{
    public static IReadOnlyList<string> Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "ApexTweaker-SelfTest-" + Guid.NewGuid().ToString("N"));
        var messages = new List<string>();

        try
        {
            var modsDirectory = Path.Combine(root, "audit", "mods");
            Directory.CreateDirectory(modsDirectory);
            CreateFabricJar(Path.Combine(modsDirectory, "sample-1.0.jar"), "sample", "1.0.0", new Dictionary<string, string>());
            CreateFabricJar(Path.Combine(modsDirectory, "sample-2.0.jar"), "sample", "2.0.0", new Dictionary<string, string>());
            CreateFabricJar(
                Path.Combine(modsDirectory, "broken.jar"),
                "broken",
                "1.0.0",
                new Dictionary<string, string> { ["missing-library"] = ">=1.0" });

            var audit = new MinecraftAuditService().Audit(modsDirectory);
            Assert(audit.Summary.DuplicateModIds == 1, "O scanner nao detectou o ID duplicado.");
            Assert(audit.Summary.MissingDependencies == 1, "O scanner nao detectou a dependencia ausente.");
            messages.Add("PASS: scanner detecta duplicidade e dependencia ausente.");

            var reportDirectory = Path.Combine(root, "reports");
            var report = new MinecraftReportService().WriteAudit(audit, reportDirectory);
            Assert(File.Exists(report.JsonPath) && File.Exists(report.MarkdownPath) && File.Exists(report.TextPath),
                "Os tres formatos de relatorio nao foram gerados.");
            Assert(File.Exists(Path.Combine(report.QuarantineSuggestionsDirectory, "quarantine-plan.json")),
                "O plano de quarentena nao foi gerado.");
            messages.Add("PASS: relatorios JSON, Markdown e TXT.");

            var instanceRoot = Path.Combine(root, "instance");
            Directory.CreateDirectory(Path.Combine(instanceRoot, "mods"));
            var optionsPath = Path.Combine(instanceRoot, "options.txt");
            const string originalOptions = "renderDistance:12\nsimulationDistance:12\nmaxFps:120\ncustomOption:keep\n";
            File.WriteAllText(optionsPath, originalOptions, new UTF8Encoding(false));

            var profileService = new MinecraftProfileService(Path.Combine(root, "backups"));
            var applied = profileService.ApplyProfile(instanceRoot, MinecraftProfileKind.Extreme4Gb);
            var changedOptions = File.ReadAllText(optionsPath);
            Assert(changedOptions.Contains("renderDistance:4", StringComparison.Ordinal), "Render distance nao foi aplicada.");
            Assert(changedOptions.Contains("simulationDistance:4", StringComparison.Ordinal), "Simulation distance nao foi aplicada.");
            Assert(changedOptions.Contains("customOption:keep", StringComparison.Ordinal), "Opcao desconhecida foi perdida.");
            Assert(Directory.Exists(applied.BackupDirectory), "Backup nao foi criado.");
            messages.Add("PASS: EXTREME_4GB preserva opcoes desconhecidas e cria backup.");

            _ = profileService.RollbackLatest(instanceRoot);
            var rolledBack = File.ReadAllText(optionsPath).Replace("\r\n", "\n");
            Assert(rolledBack == originalOptions, "Rollback nao restaurou o options.txt original.");
            Assert(!File.Exists(Path.Combine(instanceRoot, "apextweaker-java-args.txt")),
                "Rollback nao removeu o arquivo criado pelo proprio ApexTweaker.");
            messages.Add("PASS: rollback restaura bytes logicos e remove somente o arquivo gerado.");

            var view = new MinecraftView();
            view.SetSelectedPath(modsDirectory);
            Assert(view.SelectedPath == modsDirectory, "A view Cobblemon nao carregou o caminho selecionado.");
            messages.Add("PASS: XAML da pagina Cobblemon carrega em thread STA.");

            messages.Add("SELF_TEST_OK");
            return messages;
        }
        finally
        {
            var tempRoot = Path.GetFullPath(Path.GetTempPath());
            var fullRoot = Path.GetFullPath(root);
            if (fullRoot.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
            {
                Directory.Delete(fullRoot, recursive: true);
            }
        }
    }

    private static void CreateFabricJar(
        string path,
        string id,
        string version,
        IReadOnlyDictionary<string, string> dependencies)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        var entry = archive.CreateEntry("fabric.mod.json", CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        var dependencyJson = JsonSerializer.Serialize(dependencies);

        writer.Write($"{{\"schemaVersion\":1,\"id\":\"{id}\",\"name\":\"{id}\",\"version\":\"{version}\",\"environment\":\"*\",\"depends\":{dependencyJson}}}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
