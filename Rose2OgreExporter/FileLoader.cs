using System.IO;
using Revise.ZMD;
using Revise.ZMO;
using Revise.ZMS;

namespace Rose2OgreExporter
{
    public class FileLoader
    {
        public static BoneFile ReadZmd(FileInfo file)
        {
            var boneFile = new BoneFile();
            boneFile.Load(file.FullName);
            return boneFile;
        }

        public static MotionFile ReadZmo(FileInfo file)
        {
            var motionFile = new MotionFile();
            motionFile.Load(file.FullName);
            return motionFile;
        }

        public static ModelFile ReadZms(FileInfo file)
        {
            var modelFile = new ModelFile();
            modelFile.Load(file.FullName);
            return modelFile;
        }
    }
}
