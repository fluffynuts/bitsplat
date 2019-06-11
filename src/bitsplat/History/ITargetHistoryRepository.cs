namespace bitsplat.History
{
    public interface ITargetHistoryRepository
    {
        void Add(HistoryItem item);
        HistoryItem Find(string path);
        bool Exists(string path);
    }
}