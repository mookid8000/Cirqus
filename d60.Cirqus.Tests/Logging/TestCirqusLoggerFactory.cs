using System.Linq;
using d60.Cirqus.Logging;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Logging
{
    [TestFixture]
    public class TestCirqusLoggerFactory
    {
        [Test]
        public void LogsWithTheRightType()
        {
            // arrange
            var hasListOfLogStatements = new ListLoggerFactory();
            CirqusLoggerFactory.Current = hasListOfLogStatements;
            var loggingClass = new LoggingClass();

            // act
            loggingClass.LogSomething();

            // assert
            var line = hasListOfLogStatements.LoggedLines.Single();

            Assert.That(line.OwnerType, Is.EqualTo(typeof(LoggingClass)));
        }
    }

    class LoggingClass
    {
        static Logger _logger;

        static LoggingClass()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        public void LogSomething()
        {
            _logger.Info("Woot!");
        }
    }
}