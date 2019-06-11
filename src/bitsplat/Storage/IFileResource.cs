using System.IO;

namespace bitsplat.Storage
{
    public interface IFileResource
    {
        string Path { get; }
        long Size { get; }
        string RelativePath { get; }
        
        Stream Read();
        Stream Write();
    }
}