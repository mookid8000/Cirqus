using System;
using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Config
{
    /// <summary>
    /// Simple retry thingie
    /// </summary>
    public class Retryer
    {
        static Logger _logger;

        static Retryer()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// ThreadStatic because <see cref="Random"/> is not reentrant
        /// </summary>
        [ThreadStatic]
        static Random _randomizzle;

        /// <summary>
        /// Invokes the given <see cref="Action"/>, executing it up to <paramref name="maxRetries"/> times if necessary to complete
        /// successfully, throwing all of the caught exceptions in an <see cref="AggregateException"/> if it could not be completed eventually
        /// </summary>
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
                        throw new AggregateException(
                            string.Format("Could not complete the call (retried {0} times)", maxRetries),
                            caughtExceptions);
                    }

                    var millisecondsTimeout = NextRandom(10)*10;

                    _logger.Info("Attempt {0} failed: {1} - will retry in {2} ms", caughtExceptions.Count, exception, millisecondsTimeout);

                    retry = true;
                    Thread.Sleep(millisecondsTimeout);
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