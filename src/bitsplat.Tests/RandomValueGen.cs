using System.IO;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    public static class RandomValueGen
    {
        // TODO: merge this into PB
        public static string GetRandomPath(
            int minParts = 1)
        {
            return GetRandomCollection<string>(minParts, 3)
                .JoinWith(Path.DirectorySeparatorChar.ToString());
        }
    }
}