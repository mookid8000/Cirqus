using System;
using System.Diagnostics;
using d60.EventSorcerer.Numbers;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests
{
    public class FixtureBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TimeMachine.Reset();
        }

        [SetUp]
        public void SetUp()
        {
            DoSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual  void DoSetUp()
        {
        }
        protected virtual  void DoTearDown()
        {
        }

        protected void TakeTime(string description, Action action)
        {
            Console.WriteLine("Begin: {0}", description);
            var stopwatch = Stopwatch.StartNew();
            action();
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("End: {0} - elapsed: {1:0.0} s", description, elapsed.TotalSeconds);
        }
    }
}