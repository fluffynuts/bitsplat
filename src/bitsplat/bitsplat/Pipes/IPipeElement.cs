namespace bitsplat.Pipes
{
    public interface IPipeElement
    {
        void Drain();
        bool Pump();
        IPassThrough Pipe(IPassThrough pipe);
    }
}