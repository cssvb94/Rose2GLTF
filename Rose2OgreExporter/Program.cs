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
            var skeleton = new BoneFile();
            skeleton.Load(zmdFile.FullName);
            Logger.Info($"Loaded skeleton with {skeleton.Bones.Count} bones.");

            var motions = new List<MotionFile>();
            foreach (var zmoFile in zmoFiles)
            {
                var motion = new MotionFile();
                motion.Load(zmoFile.FullName);
                motions.Add(motion);
                Logger.Info($"Loaded motion with {motion.FrameCount} frames.");
            }
            var meshes = new List<ModelFile>();
            foreach (var zmsFile in zmsFiles)
            {
                var mesh = new ModelFile();
                mesh.Load(zmsFile.FullName);
                meshes.Add(mesh);
                Logger.Info($"Loaded mesh with {mesh.Vertices.Count} vertices.");
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
