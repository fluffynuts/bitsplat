using System;

namespace bitsplat.Pipes
{
    public interface IPipeElement: IDisposable
    {
        void Drain();
        bool Pump();
        IPassThrough Pipe(IPassThrough pipe);
    }
}