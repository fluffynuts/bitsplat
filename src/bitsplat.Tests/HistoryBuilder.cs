using System;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    public class HistoryBuilder : GenericBuilder<HistoryBuilder, History.HistoryItem>
    {
        public override HistoryBuilder WithRandomProps()
        {
            return base.WithRandomProps()
                .WithProp(o => o.Created = DateTime.UtcNow.TruncateMilliseconds())
                .WithProp(o => o.Modified = null);
        }
    }
}