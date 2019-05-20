using System.IO;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    public static class AutoTempFolderExtensions
    {
        public static string CreateRandomFile(this AutoTempFolder folder)
        {
            return Path.Combine(
                folder.Path,
                RandomValueGen.CreateRandomFileIn(folder.Path)
            );
        }

        public static string CreateRandomFolder(this AutoTempFolder folder)
        {
            return Path.Combine(folder.Path, RandomValueGen.CreateRandomFolderIn(folder.Path));
        }
    }
}