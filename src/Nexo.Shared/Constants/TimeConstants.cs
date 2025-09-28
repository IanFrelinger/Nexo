using System;

namespace Nexo.Shared.Constants
{
    /// <summary>
    /// Time-related constants
    /// </summary>
    public static class TimeConstants
    {
        public static readonly TimeSpan Zero = TimeSpan.Zero;
        public static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);
        public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
        public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
        public static readonly TimeSpan OneWeek = TimeSpan.FromDays(7);
        public static readonly TimeSpan OneMonth = TimeSpan.FromDays(30);
        public static readonly TimeSpan OneYear = TimeSpan.FromDays(365);
    }
}
