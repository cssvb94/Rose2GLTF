using Revise.ZMD;
using Revise.ZMO;
using Revise.ZMS;
using System.IO;
namespace Rose2OgreExporter;
public class FileLoader
{
    public static Skeleton ReadZmd(FileInfo file)
    {
        using var stream = file.OpenRead(); using var reader = new BinaryReader(stream); return new Skeleton(reader);
    }
    public static Motion ReadZmo(FileInfo file)
    {
        using var stream = file.OpenRead(); using var reader = new BinaryReader(stream); return new Motion(reader);
    }
    public static Mesh ReadZms(FileInfo file)
    {
        using var stream = file.OpenRead(); using var reader = new BinaryReader(stream); return new Mesh(reader);
    }
}
