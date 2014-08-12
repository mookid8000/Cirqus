using System;

namespace d60.Circus.Numbers
{
    /// <summary>
    /// Time machine that allows for fixing the current time to be able to test stuff
    /// </summary>
    public class TimeMachine
    {
        public static void Reset()
        {
            Time.Reset();
        }

        public static void FixCurrentTimeTo(DateTime time, bool driftSlightlyForEachCall = true)
        {
            if (time.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(string.Format("DateTime {0} has kind {1} - it must be UTC!", time, time.Kind));
            }

            Time.GetUtcNow = () =>
            {
                var timeToReturn = time;

                if (driftSlightlyForEachCall)
                {
                    // make time drift slightly in order to be able to verify that two separately obtained datetimes are not assumed to be equal
                    time = time.AddTicks(1);
                }

                return timeToReturn;
            };
        }
    }
}