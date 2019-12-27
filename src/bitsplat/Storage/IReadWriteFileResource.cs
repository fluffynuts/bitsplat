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

    public static class FileResourceExtensions
    {
        public static string FileName(this IFileResource resource)
        {
            // TODO: test on 'doze (FileResources can come from
            // the history database, where / is the separator char
            return Path.GetFileName(resource.RelativePath);
        }
    }
}