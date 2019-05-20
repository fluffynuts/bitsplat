namespace bitsplat
{
    public interface IFileResource
    {
        string Path { get; }
        long Size { get; }
        string RelativePath { get; }
    }
}