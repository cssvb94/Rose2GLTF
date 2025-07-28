using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using Revise.ZMO;
using Revise.ZMS;
using Revise.ZMD;
using Revise.STL;
using Revise.Animation;

namespace Rose2OgreExporter;
class Program
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    static async Task<int> Main(string[] args)
    {
        LogManager.Configuration = new XmlLoggingConfiguration("nlog.config");
        var rootCommand = CommandLine.Create();
        rootCommand.SetHandler(async (zmd, zmo, zms, up) =>
        {
            await Run(zmd, zmo, zms, up);
        },
            new Option<FileInfo>("--zmd"), new Option<FileInfo[]>("--zmo"),
            new Option<FileInfo[]>("--zms"), new Option<string>("--up"));

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(FileInfo zmdFile, FileInfo[] zmoFiles, FileInfo[] zmsFiles, string up)
    {
        try
        {
            var skeleton = FileLoader.ReadZmd(zmdFile);
            Logger.Info($"Loaded skeleton with {skeleton.Bones.Length} bones.");
            var motions = new List<Motion>();
            foreach (var zmoFile in zmoFiles)
            {
                var motion = FileLoader.ReadZmo(zmoFile); motions.Add(motion);
                Logger.Info($"Loaded motion with {motion.Frames} frames.");
            }
            var meshes = new List<Mesh>();
            foreach (var zmsFile in zmsFiles)
            {
                var mesh = FileLoader.ReadZms(zmsFile); meshes.Add(mesh);
                Logger.Info($"Loaded mesh with {mesh.Vertices.Length} vertices.");
            }
            var outputDirectory = new DirectoryInfo("Output"); if (!outputDirectory.Exists)
            {
                outputDirectory.Create();
            }

            var outputFileName = $"{Path.GetFileNameWithoutExtension(zmdFile.Name)}.gltf";
            var outputPath = Path.Combine(outputDirectory.FullName, outputFileName);
            GltfExporter.Export(skeleton, motions, meshes, up, outputPath);
            Logger.Info($"Exported scene to {outputPath}");
            await GltfValidator.Validate(outputPath);
            Logger.Info($"Validated {outputPath}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred during file processing.");
        }
    }
}
