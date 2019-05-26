namespace bitsplat.Pipes
{
    public interface ISink : IPipeElement
    {
        void Write(byte[] buffer, int count);
        void End();
        void SetSource(IPipeElement source);
    }
}