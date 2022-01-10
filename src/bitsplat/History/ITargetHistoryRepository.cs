using System.Collections.Generic;

namespace bitsplat.History
{
    public interface ITargetHistoryRepository
    {
        void Upsert(IHistoryItem item);
        void Upsert(IEnumerable<IHistoryItem> items);
        HistoryItem Find(string path);
        bool Exists(string path);
        IEnumerable<HistoryItem> FindAll(string match);
        string DatabaseFile { get; }
    }
}