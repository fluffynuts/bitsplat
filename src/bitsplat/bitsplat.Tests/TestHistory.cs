using System;
using System.Linq;
using System.Reflection;
using static NExpect.Expectations;
using NExpect;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestHistory
    {
        [TestCase("Id", typeof(int))]
        [TestCase("Path", typeof(string))]
        [TestCase("Size", typeof(long))]
        [TestCase("Created", typeof(DateTime))]
        [TestCase("Modified", typeof(DateTime?))]
        public void ShouldHaveProperty_(string name, Type type)
        {
            // Arrange
            var sut = typeof(History.History);
            // Act
            Expect(sut)
                .To.Have.Property(name)
                .With.Type(type);
            // Assert
        }

        [Test]
        public void PathShouldAlwaysUseForwardSlash()
        {
            // Arrange
            var sut = Create();
            var windowsPath = "foo\\bar\\quux";
            var unixPath = "foo/bar/quux";
            // Act
            sut.Path = windowsPath;
            // Assert
            Expect(sut.Path).To.Equal(unixPath);
        }

        private History.History Create()
        {
            return new History.History();
        }
    }

    public static class PropertyMatchers
    {
        public class WithType
        {
            public PropertyInfo PropertyInfo { get; set; }
            public WithType With => this; // lazy, but will do for now
            public Type ParentType { get; set; }
        }

        public static void Type(
            this WithType wt,
            Type expected)
        {
            Expect(wt.PropertyInfo.PropertyType)
                .To.Equal(expected,
                    () => $"Expected {wt.ParentType}.{wt.PropertyInfo.Name} to have type {expected}");
        }

        public static WithType Property(
            this IHave<Type> have,
            string name)
        {
            var result = new WithType();
            have.AddMatcher(actual =>
            {
                result.ParentType = actual;
                result.PropertyInfo = actual.GetProperties(
                        BindingFlags.Public | BindingFlags.Instance
                    )
                    .FirstOrDefault(pi => pi.Name == name);
                var passed = result.PropertyInfo != null;
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual} {passed.AsNot()}to have property '{name}'"
                );
            });
            return result;
        }
    }
}