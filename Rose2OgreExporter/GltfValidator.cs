using GltfValidator;
using System.IO;
using System.Threading.Tasks;
namespace Rose2OgreExporter
{
    public class GltfValidator
    {
        public static async Task Validate(string filePath)
        {
            var report = await Validator.ValidateAsync(filePath); File.WriteAllText($"{filePath}.report.json", report.ToJson());
        }
    }
}
