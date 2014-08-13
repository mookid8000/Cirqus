using System.Linq;
using d60.Cirqus.Numbers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Stubs
{
    public class TestSequenceNumberGenerator : ISequenceNumberGenerator
    {
        long _current;

        public TestSequenceNumberGenerator(long startWith = 0)
        {
            _current = startWith;
        }

        public long Next()
        {
            return _current++;
        }
    }

    [TestFixture]
    public class TestSequenceNumberGeneratorSelfTest
    {
        [Test]
        public void ItWorks()
        {
            var defaultGenerator = new TestSequenceNumberGenerator();

            var id1Numbers = new[]
            {
                defaultGenerator.Next(),
                defaultGenerator.Next(),
                defaultGenerator.Next(),
                defaultGenerator.Next(),
            };

            var id1ExpectedNumbers = Enumerable.Range(0, 4).ToArray();

            Assert.That(id1Numbers, Is.EqualTo(id1ExpectedNumbers), "{0} != {1}", string.Join(", ", id1Numbers), string.Join(", ", id1ExpectedNumbers));
        }
    }
}