using System.CommandLine;
using System.IO;

namespace Rose2OgreExporter
{
    public static class CommandLine
    {
        public static RootCommand Create()
        {
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>("--zmd", "Path to the ZMD skeleton file"),
                new Option<FileInfo[]>("--zmo", "Paths to the ZMO motion files"),
                new Option<FileInfo[]>("--zms", "Paths to the ZMS mesh files"),
                new Option<string>("--up", "Up direction (X, Y, or Z)")
            };
            return rootCommand;
        }
    }
}
