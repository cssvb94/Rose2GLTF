using System.Threading.Tasks;

namespace Rose2OgreExporter
{
    public class GltfValidator
    {
        public static async Task Validate(string path)
        {
            await GltfValidator.Validation.GltfValidator.Validate(path);
        }
    }
}
