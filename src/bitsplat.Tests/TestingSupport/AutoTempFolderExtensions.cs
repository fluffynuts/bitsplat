using System.IO;
using static PeanutButter.RandomGenerators.RandomValueGen;
using PeanutButter.Utils;

namespace bitsplat.Tests.TestingSupport
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