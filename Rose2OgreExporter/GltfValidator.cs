using System.Threading.Tasks;
using GltfValidator;

namespace Rose2OgreExporter
{
    public class GltfValidator
    {
        public static async Task Validate(string path)
        {
            await Validator.Validate(path);
        }
    }
}
