using System;
using System.Collections.Generic;
using System.Threading;

namespace d60.Cirqus.Config
{
    public class Retryer
    {
        [ThreadStatic]
        static Random _randomizzle;

        public void RetryOn<TException>(Action action, int maxRetries = 10) where TException : Exception
        {
            if (maxRetries == 0)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception exception)
                {
                    throw new AggregateException("Could not complete the call (no retries)", new[] {exception});
                }
            }

            bool retry;

            do
            {
                var caughtExceptions = new List<Exception>();
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
                        throw new AggregateException(string.Format("Could not complete the call (retried {0} times)", maxRetries), caughtExceptions);
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