using System;
using bitsplat.History;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests.TestingSupport
{
    public class HistoryBuilder : GenericBuilder<HistoryBuilder, HistoryItem>
    {
        public override HistoryBuilder WithRandomProps()
        {
            return base.WithRandomProps()
                .WithProp(o => o.Created = DateTime.UtcNow.TruncateMilliseconds())
                .WithProp(o => o.Modified = null);
        }
    }
}