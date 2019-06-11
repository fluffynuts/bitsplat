using System.IO;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    public static class AutoTempFolderExtensions
    {
        public static string CreateRandomFile(this AutoTempFolder folder)
        {
            return Path.Combine(
                folder.Path,
                CreateRandomFileIn(folder.Path)
            );
        }

        public static string CreateRandomFolder(this AutoTempFolder folder)
        {
            return Path.Combine(folder.Path, CreateRandomFolderIn(folder.Path));
        }
    }
}