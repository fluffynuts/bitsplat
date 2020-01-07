using System.Collections.Concurrent;
using System.Linq;

namespace bitsplat.Pipes
{
    public static class BufferPool
    {
        private static readonly ConcurrentBag<PooledBuffer> _buffers = new ConcurrentBag<PooledBuffer>();
        
        public static IPooledBuffer Borrow(int required)
        {
            var borrowed = _buffers.FirstOrDefault(b => b.Size >= required && b.Borrow());
            return borrowed ?? CreateNewPooledBufferFor(required);
        }

        private static IPooledBuffer CreateNewPooledBufferFor(int required)
        {
            var result = new PooledBuffer(required);
            result.Borrow();
            _buffers.Add(result);
            return result;
        }
    }
}