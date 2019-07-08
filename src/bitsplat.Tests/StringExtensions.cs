using System.IO;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    public static class StringExtensions
    {
        public static string RemoveRelativePath(
            this string path,
            string relativePath)
        {
            var result = path.RegexReplace($"{relativePath}$", "");
            result.TrimEnd(Path.DirectorySeparatorChar);
            return result;
        }
    }
}