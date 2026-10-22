using System;
using System.Collections.Generic;

namespace bitsplat.Tests
{
    public class FakeDelayer : IDelayer
    {
        public List<TimeSpan> Delays { get; } = new List<TimeSpan>();

        public void Delay(TimeSpan duration)
        {
            Delays.Add(duration);
        }
    }
}
