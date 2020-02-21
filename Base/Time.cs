using System;

namespace Heleus.Base
{
    public static class Time
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime TimeStampToDateTime(long timestamp)
        {
            return Epoch.AddMilliseconds(timestamp);
        }

        public static string DateTimeString(long timestamp, bool localTime = true)
        {
            var date = TimeStampToDateTime(timestamp);
            if(localTime)
                date.ToLocalTime();

            return $"{date.ToShortDateString()}  {date.ToShortTimeString()}";
        }

        public static string TimeString(long timestamp, bool localTime = true)
        {
            var date = TimeStampToDateTime(timestamp);
            if (localTime)
                date.ToLocalTime();

            return date.ToShortTimeString();
        }

        public static string DateString(long timestamp, bool localTime = true)
        {
            var date = TimeStampToDateTime(timestamp);
            if (localTime)
                date.ToLocalTime();

            return date.ToShortDateString();
        }

        public static long DateTimeToTimeStamp(DateTime value)
        {
            var elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalMilliseconds;
        }

        public static float PassedSeconds(long now, long timestamp)
        {
            return (now - timestamp) / 1000f;
        }

        public static float PassedSeconds(long timestamp)
        {
            return PassedSeconds(Timestamp, timestamp);
        }

        public static float PassedMinutes(long now, long timestamp)
        {
            return (now - timestamp) / (1000f * 60f);
        }

        public static float PassedMinutes(long timestamp)
        {
            return PassedMinutes(Timestamp, timestamp);
        }

        public static float PassedHours(long now, long timestamp)
        {
            return (now - timestamp) / (1000f * 60f * 60f);
        }

        public static float PassedHours(long timestamp)
        {
            return PassedHours(Timestamp, timestamp);
        }

        public static float PassedDays(long now, long timestamp)
        {
            return (now - timestamp) / (1000f * 60f * 60f * 24f);
        }

        public static float PassedDays(long timestamp)
        {
            return PassedDays(Timestamp, timestamp);
        }

        public static int Seconds(int seconds)
        {
            return 1000 * seconds;
        }

        public static int Minutes(int minutes)
        {
            return 1000 * 60 * minutes;
        }

        public static int Hours(int hours)
        {
            return 1000 * 60 * 60 * hours;
        }

        public static int Hours(double hours)
        {
            return (int)(1000 * 60 * 60 * hours);
        }

        public static double ToHours(long time)
        {
            return time / (1000.0 * 60 * 60);
        }

        public static double ToHours(double time)
        {
            return time / (1000.0 * 60 * 60);
        }

        public static int Days(int days)
        {
            return 1000 * 60 * 60 * 24 * days;
        }

        public static int Days(double days)
        {
            return (int)(1000 * 60 * 60 * 24 * days);
        }

        public static double ToDays(long time)
        {
            return time / (1000.0 * 60 * 60 * 24);
        }

        public static double ToDays(double time)
        {
            return time / (1000.0 * 60 * 60 * 24);
        }

        public static long Timestamp
        {
            get
            {
                return DateTimeToTimeStamp(DateTime.UtcNow);
            }
        }

        public static long UnixTimeStampUTC
        {
            get
            {
                return DateTimeToTimeStamp(DateTime.UtcNow) / 1000;
            }
        }
    }
}
