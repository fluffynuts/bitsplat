using System;

namespace bitsplat.History
{
    public interface IHistoryResource
    {
        string Path { get; set; }
        long Size { get; set; }
    }

    public class History: IHistoryResource
    {
        public int Id { get; set; }

        public string Path
        {
            get => _path;
            set => _path = Unixify(value);
        }

        private string Unixify(string value)
        {
            return value?.Replace("\\", "/");
        }

        private string _path;
        
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        public History()
        {
        }

        public History(
            string path,
            long size)
        {
            Path = path;
            Size = size;
        }
    }
}