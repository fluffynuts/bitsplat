using System.IO;

namespace bitsplat.Storage
{
    public interface IFileResource
    {
        string Path { get; }
        long Size { get; }
        string RelativePath { get; }
    }

    public interface IReadWriteFileResource: IFileResource
    {
        Stream Read();
        Stream Write();
    }
}