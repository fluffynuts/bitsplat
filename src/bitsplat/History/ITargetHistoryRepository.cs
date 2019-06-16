namespace bitsplat.History
{
    public interface ITargetHistoryRepository
    {
        void Upsert(History item);
        History Find(string path);
        bool Exists(string path);
    }
}