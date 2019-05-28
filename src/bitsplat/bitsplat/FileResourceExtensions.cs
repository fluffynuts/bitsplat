using System;
using System.Linq;
using bitsplat.Storage;

namespace bitsplat
{
    public static class FileResourceExtensions
    {
        public static bool Matches(
            this IFileResource source,
            IFileResource other)
        {
            if (source == null ||
                other == null)
            {
                return false;
            }

            return Matchers.Aggregate(
                true,
                (
                    acc,
                    cur) => acc && cur(source, other)
            );
        }

        private static Func<IFileResource, IFileResource, bool>[] Matchers =
        {
            ShouldHaveSameRelativePath,
            ShouldHaveSameSize
            // TODO: partial data check: sample source and other to look for
            // easy mismatches, which should be spottable on same-size media
            // files with 2 or 3 512-byte chunks taken at random, if the sizes
            // and names match
        };

        private static bool ShouldHaveSameSize(
            IFileResource arg1,
            IFileResource arg2)
        {
            return arg1.Size == arg2.Size;
        }

        private static bool ShouldHaveSameRelativePath(
            IFileResource arg1,
            IFileResource arg2)
        {
            return arg1.RelativePath == arg2.RelativePath;
        }
    }
}