using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;
using System.Collections.Generic;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestLinkedList
    {
        [Test]
        public void CreatingAClosedLoop()
        {
            // Arrange
            var list = new LinkedList<string>("1");
            // Act
            list.Add("2")
                .Add("3");
            list.Close();
            var result = new List<string>
            {
                list.Step(),
                list.Step(),
                list.Step(),
                list.Step()
            };
            // Assert
            Expect(result)
                .To.Equal(new[] { "1", "2", "3", "1" });
        }

        [Test]
        public void MovingForwardThroughCollection()
        {
            // Arrange
            var list = new LinkedList<string>("1");
            list.Add("2")
                .Add("3");
            // Act
            var result = new List<string>();
            foreach (var item in list)
            {
                result.Add(item);
            }
            // Assert
            Expect(result)
                .To.Equal(new[] { "1", "2", "3" });
        }
    }
}