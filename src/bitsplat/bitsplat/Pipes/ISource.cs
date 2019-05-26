namespace bitsplat.Pipes
{
    public interface ISource : IPipeElement
    {
        ISink Pipe(ISink sink);
    }
}