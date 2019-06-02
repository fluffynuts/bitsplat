namespace bitsplat.Pipes
{
    public interface IPassThrough
        : ISource,
          ISink
    {
        new IPassThrough Pipe(IPassThrough pipe);
    }
}