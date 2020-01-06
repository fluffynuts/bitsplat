using System.Collections.Generic;

namespace bitsplat.History
{
    public class NullTargetHistoryRepository : ITargetHistoryRepository
    {
        public void Upsert(IHistoryItem item)
        {
        }

        public void Upsert(IEnumerable<IHistoryItem> items)
        {
        }

        public HistoryItem Find(string path)
        {
            return null;
        }

        public bool Exists(string path)
        {
            return false;
        }

        public IEnumerable<HistoryItem> FindAll(string match)
        {
            return new HistoryItem[0];
        }
    }
}