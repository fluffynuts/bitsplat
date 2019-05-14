using System;

namespace bitsplat
{
    public class History
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}