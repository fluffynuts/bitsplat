namespace bitsplat.History
{
    public interface ITargetHistoryRepository
    {
        void Upsert(IHistoryResource item);
        History Find(string path);
        bool Exists(string path);
    }
}