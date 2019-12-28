using bitsplat.Storage;

namespace bitsplat.Tests.History
{
    public class FakeFileResource : BasicFileResource, IFileResource
    {
        public override string Path { get; }
        public override long Size { get; }
        public override string RelativePath { get; }

        public override string ToString()
        {
            return $"{Path} :: {Size}";
        }

        public static FakeFileResource For(
            string basePath,
            string relativePath,
            long size)
        {
            return new FakeFileResource(
                basePath,
                relativePath,
                size);
        }

        public FakeFileResource(
            string basePath,
            string relativePath,
            long size)
        {
            RelativePath = relativePath;
            Path = System.IO.Path.Combine(basePath, relativePath);
            Size = size;
        }
    }
}