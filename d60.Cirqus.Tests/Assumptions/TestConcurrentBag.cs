using System.Collections.Concurrent;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Assumptions
{
    [TestFixture]
    public class TestConcurrentBag
    {
        [Test, Ignore("oh, it does")]
        public void DoesNotContainMultipleInstancesOfTheSameObject()
        {
            var baggerino = new ConcurrentBag<string>();

            baggerino.Add("hej");
            baggerino.Add("med");
            baggerino.Add("dig");
            baggerino.Add("dig");
            baggerino.Add("dig");
            baggerino.Add("dig");

            Assert.That(baggerino.Count, Is.EqualTo(3));
        }
    }
}