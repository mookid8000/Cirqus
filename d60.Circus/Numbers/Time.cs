using System;

namespace d60.Circus.Numbers
{
    /// <summary>
    /// Gets the current time as it should be: in UTC :)
    /// </summary>
    public class Time
    {
        public static DateTime UtcNow()
        {
            return GetUtcNow();
        }

        internal static Func<DateTime> OriginalGetUtcNow = () => DateTime.UtcNow;
        internal static Func<DateTime> GetUtcNow = OriginalGetUtcNow;
        internal static void Reset()
        {
            GetUtcNow = OriginalGetUtcNow;
        }
    }
}