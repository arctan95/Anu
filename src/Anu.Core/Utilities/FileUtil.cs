using System.IO;
using System.Threading.Tasks;

namespace Anu.Core.Utilities;

public static class FileUtil
{
    public static async Task SaveFileAsync(string filePath, string text)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            await File.WriteAllTextAsync(filePath, text);
        }
    }
}