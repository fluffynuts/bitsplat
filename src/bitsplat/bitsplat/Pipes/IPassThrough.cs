namespace bitsplat.Pipes
{
    public interface IPassThrough
        : ISource,
          ISink
    {
        IPassThrough Pipe(IPassThrough pipe);
    }
}