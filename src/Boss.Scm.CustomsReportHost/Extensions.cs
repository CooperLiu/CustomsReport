using System;

namespace Boss.Scm.CustomsReportHost
{
    static class Extensions
    {
        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式(毫秒)
        /// </summary>
        /// <param name="time"> DateTime时间格式</param>
        /// <returns>Unix时间戳格式</returns>
        public static long ToUnixTimeStampMillis(this System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (long)(time - startTime).TotalMilliseconds;
        }

        /// <summary>
        /// Unix时间戳格式(毫秒)转换为DateTime时间格式
        /// </summary>
        /// <param name="unixTimeStampMillis"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromUnixTimeStampMillis(this long unixTimeStampMillis)
        {
            var startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return startDateTime.AddMilliseconds(unixTimeStampMillis).ToLocalTime();
        }
    }
}