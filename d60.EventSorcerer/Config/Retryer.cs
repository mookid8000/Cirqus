using System;
using System.Collections.Generic;
using System.Threading;

namespace d60.EventSorcerer.Config
{
    public class Retryer
    {
        [ThreadStatic]
        static Random _randomizzle;

        public void RetryOn<TException>(Action action, int maxRetries = 10) where TException : Exception
        {
            bool retry;
            var caughtExceptions = new List<Exception>();

            do
            {
                try
                {
                    action();
                    retry = false;
                }
                catch (TException exception)
                {
                    caughtExceptions.Add(exception);

                    if (caughtExceptions.Count >= maxRetries)
                    {
                        throw new AggregateException(string.Format("Could not complete the call, even after {0} attempts", maxRetries), caughtExceptions);
                    }

                    retry = true;
                    Thread.Sleep(NextRandom(200));
                }
            } while (retry);
        }

        static int NextRandom(int max)
        {
            if (_randomizzle == null)
            {
                _randomizzle = new Random(DateTime.Now.GetHashCode());
            }

            return _randomizzle.Next(max);
        }
    }
}