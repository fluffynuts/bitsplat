namespace bitsplat
{
    public interface ITargetHistoryRepository
    {
        void Add(History item);
        History Find(string path);
        bool Exists(string path);
    }
}