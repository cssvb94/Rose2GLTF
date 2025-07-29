using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using GltfValidator.Validation;
using NLog;
using NLog.Config;
using Revise.ZMO;
using Revise.ZMS;
using Revise.ZMD;

namespace Rose2OgreExporter;
class Program
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    static async Task<int> Main(string[] args)
    {
        LogManager.Configuration = new XmlLoggingConfiguration("nlog.config");
        var rootCommand = new RootCommand
        {
            new Option<FileInfo>("--zmd", "Path to the ZMD skeleton file"),
            new Option<FileInfo[]>("--zmo", "Paths to the ZMO motion files"),
            new Option<FileInfo[]>("--zms", "Paths to the ZMS mesh files"),
            new Option<string>("--up", "Up direction (X, Y, or Z)")
        };

        rootCommand.SetHandler(async (zmd, zmo, zms, up) =>
        {
            await Run(zmd, zmo, zms, up);
        },
            rootCommand.Options[0] as Option<FileInfo>,
            rootCommand.Options[1] as Option<FileInfo[]>,
            rootCommand.Options[2] as Option<FileInfo[]>,
            rootCommand.Options[3] as Option<string>);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(FileInfo zmdFile, FileInfo[] zmoFiles, FileInfo[] zmsFiles, string up)
    {
        try
        {
            var skeleton = FileLoader.ReadZmd(zmdFile);
            Logger.Info($"Loaded skeleton with {skeleton.Bones.Count} bones.");

            var motions = new List<MotionFile>();
            foreach (var zmoFile in zmoFiles)
            {
                var motion = FileLoader.ReadZmo(zmoFile);
                motions.Add(motion);
                Logger.Info($"Loaded motion with {motion.FrameCount} frames.");
            }
            var meshes = new List<ModelFile>();
            foreach (var zmsFile in zmsFiles)
            {
                var mesh = FileLoader.ReadZms(zmsFile);
                meshes.Add(mesh);
                Logger.Info($"Loaded mesh with {mesh.Vertices.Count} vertices.");
            }
            var outputDirectory = new DirectoryInfo("Output");
            if (!outputDirectory.Exists)
            {
                outputDirectory.Create();
            }

            var outputFileName = $"{Path.GetFileNameWithoutExtension(zmdFile.Name)}.gltf";
            var outputPath = Path.Combine(outputDirectory.FullName, outputFileName);
            GltfExporter.Export(skeleton, motions, meshes, up, outputPath);
            Logger.Info($"Exported scene to {outputPath}");

            var validationResult = await Validator.Validate(outputPath);
            if (validationResult.Issues.Count > 0)
            {
                Logger.Error("glTF validation failed:");
                foreach (var issue in validationResult.Issues)
                {
                    Logger.Error($"  - {issue.Message}");
                }
            }
            else
            {
                Logger.Info("glTF validation successful.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred during file processing.");
        }
    }
}
