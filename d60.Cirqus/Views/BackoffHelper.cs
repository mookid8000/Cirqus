using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace d60.Cirqus.Views
{
    class BackoffHelper
    {
        readonly TimeSpan[] _backoffTimes;

        int _currentBackoffTimeIndex;

        public BackoffHelper(IEnumerable<TimeSpan> backoffTimes)
        {
            if (backoffTimes == null) throw new ArgumentNullException("backoffTimes");
            _backoffTimes = backoffTimes.ToArray();
            if (_backoffTimes.Length == 0)
            {
                throw new ArgumentException("Please add at least one backoff time!");
            }
        }

        public TimeSpan GetTimeToWait()
        {
            var index = Interlocked.Increment(ref _currentBackoffTimeIndex);

            return _backoffTimes[Math.Min(_backoffTimes.Length - 1, index)];
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _currentBackoffTimeIndex, 0);
        }
    }
}