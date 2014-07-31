using System;
using System.Diagnostics;
using System.Timers;
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

        public delegate void TimerCallback(TimeSpan elapsedTotal);

        protected void TakeTime(string description, Action action, TimerCallback periodicCallback = null)
        {
            Console.WriteLine("Begin: {0}", description);
            var stopwatch = Stopwatch.StartNew();
            var lastCallback = DateTime.UtcNow;

            using (var timer = new Timer())
            {
                if (periodicCallback != null)
                {
                    timer.Interval = 5000;
                    timer.Elapsed += delegate
                    {
                        periodicCallback(stopwatch.Elapsed);
                    };
                    timer.Start();
                }

                action();
            }
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("End: {0} - elapsed: {1:0.0} s", description, elapsed.TotalSeconds);
        }
    }
}