namespace bitsplat.History
{
    public interface ITargetHistoryRepository
    {
        void Upsert(IHistoryItem item);
        HistoryItem Find(string path);
        bool Exists(string path);
    }
}