using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Numbers;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class TestSequenceNumberGenerator : ISequenceNumberGenerator
    {
        readonly int _startWith;
        readonly Dictionary<Guid, int> _numbers = new Dictionary<Guid, int>();

        public TestSequenceNumberGenerator(int startWith = 0)
        {
            _startWith = startWith;
        }

        public int Next(Guid aggregateRootId)
        {
            lock (_numbers)
            {
                if (!_numbers.ContainsKey(aggregateRootId))
                {
                    _numbers[aggregateRootId] = _startWith+1;

                    return _startWith;
                }

                var numberToReturn = _numbers[aggregateRootId];
                _numbers[aggregateRootId] = numberToReturn + 1;
                return numberToReturn;
            }
        }
    }

    [TestFixture]
    public class TestSequenceNumberGeneratorSelfTest
    {
        [Test]
        public void ItWorks()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var defaultGenerator = new TestSequenceNumberGenerator();

            var id1Numbers = new[]
            {
                defaultGenerator.Next(id1),
                defaultGenerator.Next(id1),
                defaultGenerator.Next(id1),
                defaultGenerator.Next(id1),
            };

            var id2Numbers = new[]
            {
                defaultGenerator.Next(id2),
                defaultGenerator.Next(id2),
                defaultGenerator.Next(id2),
                defaultGenerator.Next(id2),
                defaultGenerator.Next(id2),
                defaultGenerator.Next(id2),
            };

            var id1ExpectedNumbers = Enumerable.Range(0, 4).ToArray();
            var id2ExpectedNumbers = Enumerable.Range(0, 6).ToArray();

            Assert.That(id1Numbers, Is.EqualTo(id1ExpectedNumbers), "{0} != {1}", string.Join(", ", id1Numbers), string.Join(", ", id1ExpectedNumbers));
            Assert.That(id2Numbers, Is.EqualTo(id2ExpectedNumbers), "{0} != {1}", string.Join(", ", id2Numbers), string.Join(", ", id2ExpectedNumbers));
        }
    }
}