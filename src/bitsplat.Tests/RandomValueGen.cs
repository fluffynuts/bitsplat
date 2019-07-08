using System.IO;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    public static class RandomValueGen
    {
        // TODO: merge this into PB
        public static string GetRandomPath()
        {
            return PeanutButter.RandomGenerators.RandomValueGen.GetRandomCollection<string>(1, 3)
                .JoinWith(Path.DirectorySeparatorChar.ToString());
        }
    }
}