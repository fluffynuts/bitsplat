using System.IO;

namespace bitsplat
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