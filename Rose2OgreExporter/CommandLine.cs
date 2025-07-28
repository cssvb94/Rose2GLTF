using System.CommandLine;
namespace Rose2OgreExporter
{
    public class CommandLine
    {
        public static RootCommand Create()
        {
            var zmdOption = new Option<FileInfo>(name: "--zmd", description: "The ZMD file to process.")
            {
                IsRequired = true
            }; var zmoOption = new Option<FileInfo[]>(name: "--zmo", description: "The ZMO files to process.")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };
            var zmsOption = new Option<FileInfo[]>(name: "--zms", description: "The ZMS files to process.")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };
            var upOption = new Option<string>(name: "--up", description: "The up direction for the exported GLTF file.", getDefaultValue: () => "Y");
            var rootCommand = new RootCommand("Rose2Ogre Exporter");
            rootCommand.AddOption(zmdOption);
            rootCommand.AddOption(zmoOption);
            rootCommand.AddOption(zmsOption);
            rootCommand.AddOption(upOption);
            return rootCommand;
        }
    }
}
