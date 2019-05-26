namespace bitsplat.Pipes
{
    public class NullPassThrough : PassThrough
    {
        protected override void OnWrite(
            byte[] buffer,
            int count)
        {
        }

        protected override void OnEnd()
        {
        }
    }
}