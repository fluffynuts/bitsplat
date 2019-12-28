using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace bitsplat.Storage
{
    public interface IFileResource
    {
        string Path { get; }
        long Size { get; }
        string RelativePath { get; }
        string Name { get; }
    }

    public abstract class BasicFileResource : IFileResource
    {
        public abstract string Path { get; }
        public abstract long Size { get; }
        public abstract string RelativePath { get; }

        public virtual string Name => Regex.Split(
                RelativePath ?? "",
                "[/||\\\\]"
            )
            .Last();
    }

    public interface IReadWriteFileResource : IFileResource
    {
        Stream OpenForRead();
        Stream OpenForWrite();
    }
}