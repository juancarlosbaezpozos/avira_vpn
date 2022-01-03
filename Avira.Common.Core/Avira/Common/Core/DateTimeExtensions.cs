using System;

namespace Avira.Common.Core
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeStamp(this DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
    }
}