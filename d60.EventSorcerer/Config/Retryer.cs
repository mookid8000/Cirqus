using System;
using System.Threading;

namespace d60.EventSorcerer.Config
{
    public class Retryer
    {
        [ThreadStatic]
        static Random _randomizzle;

        public static void RetryOn<TException>(Action action) where TException : Exception
        {
            bool retry;

            do
            {
                try
                {
                    action();
                    retry = false;
                }
                catch (TException)
                {
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