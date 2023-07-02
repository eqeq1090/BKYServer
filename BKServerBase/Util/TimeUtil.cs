using System.Diagnostics;
using BKServerBase.ConstEnum;

namespace BKServerBase.Util
{
    public class TimeUtil
    {
        private static Stopwatch? m_TickBase = null;
        public static void InitTickBase()
        {
            m_TickBase = new Stopwatch();
            m_TickBase.Start();
        }

        public static DateTime ToDateTime(long timestamp)
        {
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return result.AddSeconds(timestamp).ToLocalTime();
        }

        public static DateTime GetTimestamp(bool useUTC = true)
        {
            if (useUTC == true)
            {
                return DateTime.UtcNow;
            }
            else
            {
                return DateTime.Now;
            }
        }

        public static long GetTimeStampInMilliSec(bool useUTC = true)
        {
            if (useUTC == true)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            else
            {
                return DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        public static long GetTimeStampDiffInMilliSec(DateTime prev, DateTime next)
        {
            return (long)(next - prev).TotalMilliseconds;
        }

        public static long GetTimeStampDiffInSec(DateTime prev, DateTime next)
        {
            return (long)(next - prev).TotalSeconds;
        }

        public static long GetTargetTimeStampInMilliSec(DateTime time)
        {
            return ((DateTimeOffset)time).ToUnixTimeMilliseconds();
        }

        public static long GetTimeStampDiffMilliSec(long value)
        {
            return Math.Abs(GetTimeStampInMilliSec() - value);
        }

        public static long GetTargetTimestampOfTodayMillisec(int hour, int minute, int second, bool useUTC = true)
        {
            var now = GetTimestamp();
            return GetTargetTimeStampInMilliSec(new DateTime(now.Year, now.Month, now.Day, hour, minute, second, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local).ToUniversalTime());
        }

        public static DateTime GetTargetTimestampOfToday(int hour, int minute, int second, bool useUTC = true)
        {
            var now = GetTimestamp();
            return new DateTime(now.Year, now.Month, now.Day, hour, minute, second, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        public static DateTime GetMidnightBefore(int dayCount, bool useUTC = true)
        {
            var date = GetTimestamp().AddDays(-dayCount);
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        public static bool IsInPeriod((int hour, int minute, int second) start, (int hour, int minute, int second) end, bool useUTC = true)
        {
            var now = GetTimestamp();
            var startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local).AddHours(start.hour).AddMinutes(start.minute).AddSeconds(start.second).ToUniversalTime();
            var endTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local).AddHours(end.hour).AddMinutes(end.minute).AddSeconds(end.second).ToUniversalTime();
            var startTimeStamp = GetTargetTimeStampInMilliSec(startTime);
            var endTimeStamp = GetTargetTimeStampInMilliSec(endTime);
            var currentTimeStamp = GetTimeStampInMilliSec();
            return (currentTimeStamp >= startTimeStamp && currentTimeStamp <= endTimeStamp);
        }

        public static bool IsAfter(DateTime timestamp)
        {
            return GetTimestamp() >= timestamp;
        }

        public static bool IsBefore(DateTime timestamp)
        {
            return GetTimestamp() <= timestamp;
        }

        public static bool IsBefore(DateTime sourceTime, DateTime destTime)
        {
            return destTime <= sourceTime;
        }

        public static bool IsToday(DateTime timestamp)
        {
            var now = GetTimestamp();
            return (now.Year == timestamp.Year &&
                now.Month == timestamp.Month &&
                now.Day == timestamp.Day);
        }

        public static long GetRemainTimeOffset(int targetHour, int targetMinute, int offsetMinute, bool useUTC = true)
        {
            var now = GetTimestamp();
            var targetTime = GetTargetTimeStampInMilliSec(new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, useUTC == true ? DateTimeKind.Utc : DateTimeKind.Local).AddHours(targetHour).AddMinutes(targetMinute + offsetMinute).
            ToUniversalTime());
            var current = GetTimeStampInMilliSec();
            return targetTime > current ? targetTime - current : 0;
        }

        public static float ChangeTimeMilli2Sec(long timeMilliSec)
        {
            return Convert.ToSingle(timeMilliSec * BaseConsts.MILLISEC_TO_SEC_MAGNIFICANT);
        }

        public static long ChangeTimeSec2Milli(int timeSec)
        {
            return timeSec * BaseConsts.SEC_TO_MILLISEC_MAGNIFICANT;
        }

        public static long ChangeTimeSec2Milli(float timeSec)
        {
            return (long)(timeSec * BaseConsts.SEC_TO_MILLISEC_MAGNIFICANT);
        }

        public static long ChangeTimeSec2Milli(double timeSec)
        {
            return (long)(timeSec * BaseConsts.SEC_TO_MILLISEC_MAGNIFICANT);
        }

        public static long GetCurrentTickMilliSec()
        {
            return m_TickBase!.ElapsedMilliseconds;
        }

        public static long GetCurrentTickDiffMilliSec(long value)
        {
            return GetCurrentTickMilliSec() - value;
        }

        public static long GetCurrentTickSec()
        {
            return m_TickBase!.ElapsedMilliseconds / 1000;
        }

        public static long GetRemainTime(long duration, long startTime)
        {
            return duration - (GetCurrentTickMilliSec() - startTime);
        }

        public static long GetElapsedTime(long startTime)
        {
            return GetCurrentTickMilliSec() - startTime;
        }

        public static double GetCurrentTickMilliSecDetail()
        {
            return m_TickBase!.Elapsed.TotalMilliseconds;
        }
    }
}
