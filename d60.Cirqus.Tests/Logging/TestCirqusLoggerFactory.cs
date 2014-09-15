using System.Linq;
using d60.Cirqus.Logging;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Logging
{
    [TestFixture]
    public class TestCirqusLoggerFactory
    {
        static Logger _logger;

        static TestCirqusLoggerFactory()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        [Test]
        public void LogsWithTheRightType()
        {
            // arrange
            var hasListOfLogStatements = new ListLoggerFactory();
            CirqusLoggerFactory.Current = hasListOfLogStatements;

            // act
            _logger.Info("Woot!");

            // assert
            var line = hasListOfLogStatements.LoggedLines.Single();

            Assert.That(line.OwnerType, Is.EqualTo(typeof(TestCirqusLoggerFactory)));
        }
    }
}