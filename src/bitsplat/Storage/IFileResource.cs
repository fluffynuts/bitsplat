using System.IO;

namespace bitsplat.Storage
{
    public interface IFileResourceProperties
    {
        string Path { get; }
        long Size { get; }
        string RelativePath { get; }
    }

    public interface IFileResource: IFileResourceProperties
    {
        Stream Read();
        Stream Write();
    }
}